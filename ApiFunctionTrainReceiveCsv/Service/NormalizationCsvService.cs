using ApiFunctionTrainReceiveCsv.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace ApiFunctionTrainReceiveCsv.Service
{
    public class NormalizationCsvService
    {
        private readonly string[] TimestampFormats =
      {
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy/MM/dd HH:mm:ss",
        "dd/MM/yyyy HH:mm",
        "dd/MM/yyyy HH:mm:ss"
    };

        private string _connectionString;

        public NormalizationCsvService(IConfiguration configuration) 
        {
            _connectionString = configuration.GetConnectionString("db") ?? throw new Exception("Stringa non trovata");
        }

        public async Task NormilizeCsvStream(Stream blobStream)
        {
            try
            {
                using var reader = new StreamReader(blobStream);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    HeaderValidated = null
                });
                await DeleteRow();
                var records = new List<RecordTelemetria>();
                await foreach (var record in csv.GetRecordsAsync<RecordTelemetria>())
                {
                    records.Add(record);
                }
                var mediatemperatuta = records.Where(a => a.TemperaturaMotoreC != null && a.TemperaturaMotoreC < 175 && a.TemperaturaMotoreC > 0)
                                                  .Select(a => a.TemperaturaMotoreC)
                                                  .Average();

                var mediavibrazione = records.Where(a => a.VibrazioneMmS != null && a.VibrazioneMmS > 0).Select(a => a.VibrazioneMmS).Average();

                var mediadeformazione = records.Where(a => a.DeformazioneBinarioMm != null && a.DeformazioneBinarioMm > 0)
                                            .Select(a => a.DeformazioneBinarioMm).Average();

                var mediaVelocita = records
                                    .Where(a => !string.IsNullOrWhiteSpace(a.VelocitaRaw)) 
                                    .Select(a => {
                                        string cleanValue = a.VelocitaRaw.Replace("kmh", "", StringComparison.OrdinalIgnoreCase).Trim();

        
                                        if (double.TryParse(cleanValue, CultureInfo.InvariantCulture, out double result))
                                            return (double?)result;

                                        return null;
                                    })
                                    .Where(v => v.HasValue) 
                                    .Average(v => v.Value);


                var mediaRitardoMinuti = records.Where(a => a.RitardoMinuti > 0 && a.RitardoMinuti < 300 && a.RitardoMinuti != null)
                                                .Select(a => a.RitardoMinuti).
                                                Average();

                var media_anomaly_score = records.Where(a => a.AnomalyScore > 0 && a.AnomalyScore != null)
                                            .Select(a => a.AnomalyScore).Average();
                var conteggioSeverita = records
                    .Where(r => !string.IsNullOrWhiteSpace(r.LivelloSeverita) && !r.LivelloSeverita.Contains("n/d") && !r.LivelloSeverita.Contains("nessuna") && !r.LivelloSeverita.Contains("?"))
                    .GroupBy(r => r.LivelloSeverita.ToLower().Trim())
                    .ToDictionary(g => g.Key, g => g.Count() / 5);
                int tempValue = 0;

                foreach (var conteggio in conteggioSeverita)
                {
                    tempValue += conteggio.Value;
                    string key = conteggio.Key;
                    conteggioSeverita[key] = tempValue;
                }

                var totalePercentualeConteggioServerità = records.Where(r => !string.IsNullOrWhiteSpace(r.LivelloSeverita)).Count();


                var random = new Random();

                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    if (record == null) continue;
                    var resultTimeStamp = await ParseTimestamp(record.Timestamp);
                    if (resultTimeStamp != null)
                    {
                        record.Timestamp = resultTimeStamp.ToString();
                        record.TimestampParsed = resultTimeStamp;
                    }
                    else
                    {
                        int index = -1;
                        while (resultTimeStamp is null)
                        {
                            resultTimeStamp =  records[i + index].TimestampParsed;
                            index--;
                        }

                        record.TimestampParsed = resultTimeStamp;

                    }
                    var resultVelocità = await PulisciVelocita(record.VelocitaRaw);
                    if(resultVelocità != null)
                    {
                        record.VelocitaRaw = resultVelocità.ToString();

                        record.VelocitaKmh = resultVelocità;
                    }
                    else
                    {
                        record.VelocitaKmh = mediaVelocita;
                    }
                  

                    if(record.TemperaturaMotoreC == null || record.TemperaturaMotoreC > 175 || record.TemperaturaMotoreC < 0)
                    {
                        record.TemperaturaMotoreC = mediatemperatuta;
                    }

                    if(record.VibrazioneMmS == null)
                    {
                        record.VibrazioneMmS = mediavibrazione;
                    }

                    if(record.DeformazioneBinarioMm == null || record.DeformazioneBinarioMm < 0)
                    {
                        record.DeformazioneBinarioMm = mediadeformazione;
                    }

                    if(record.RitardoMinuti == null || record.RitardoMinuti > 300)
                    {
                        record.RitardoMinuti = ((int?)mediaRitardoMinuti);
                    }
                    else if(record.RitardoMinuti < 0)
                    {
                        record.RitardoMinuti = 0;
                    }

                    if(record.AnomalyScore == null || record.AnomalyScore <= 0)
                    {
                        record.AnomalyScore = media_anomaly_score;
                    }


                    if (!string.IsNullOrEmpty(record.TipoEvento)) 

                    {
                        if (record.TipoEvento.Contains("nessuno", StringComparison.OrdinalIgnoreCase))
                        {
                            record.LivelloSeverita = "nessuna";
                        }

                        if (string.IsNullOrEmpty(record.LivelloSeverita) || record.LivelloSeverita.Contains("?") || record.LivelloSeverita.Contains("n/d"))
                        {

                            int numrand = random.Next(totalePercentualeConteggioServerità/5);


                            string nomedamette = string.Empty;
                            foreach(var conteggio in conteggioSeverita)
                            {
                                if(conteggio.Value < numrand)
                                {
                                    nomedamette = conteggio.Key;
                                }
                            }

                            record.LivelloSeverita = nomedamette;

                        }
                        
                    }
                    else
                    {
                        record.TipoEvento = "nessuno";
                        record.LivelloSeverita = "nessuna";
                    }
                    
                }

                using var connection = new SqlConnection(_connectionString);
                string query = $"""
                IF NOT EXISTS (SELECT 1 FROM Tracciati WHERE track_id = @TrackId)
                INSERT INTO Tracciati (track_id, tratta)
                VALUES (@TrackId,@Tratta);
                
                IF NOT EXISTS (SELECT 1 FROM Treni WHERE train_id = @TrainId)
                INSERT INTO Treni (train_id, nome_treno, linea)
                VALUES (@TrainId,@NomeTreno, @Linea);

                IF NOT EXISTS (SELECT 1 FROM RecordTelemetria WHERE record_id = @RecordId)
                INSERT INTO RecordTelemetria (
                    record_id, timestamp, train_id, track_id,
                    stazione_riferimento, latitudine, longitudine,
                    velocita_kmh, accelerazione_ms2, temperatura_motore_c,
                    pressione_freni_bar, vibrazione_mm_s, deformazione_binario_mm,
                    temperatura_rotaia_c, tipo_evento, ritardo_minuti,
                    livello_severita, anomaly_score, fonte_dato)
                VALUES (
                    @RecordId, @TimestampParsed, 
                    (SELECT id FROM Treni WHERE train_id = @TrainId), 
                    (SELECT id FROM Tracciati WHERE track_id = @TrackId),
                    @StazioneRiferimento, @Latitudine, @Longitudine,
                    @VelocitaKmh, @AccelerazioneMs2, @TemperaturaMotoreC,
                    @PressioneFreniBar, @VibrazioneMmS, @DeformazioneBinarioMm,
                    @TemperaturaRotaiaC, @TipoEvento, @RitardoMinuti,
                    @LivelloSeverita, @AnomalyScore, @FonteDato);
                """;
                var _result = await connection.ExecuteAsync(query,records);
                if (_result > 0)
                {
                    Console.Write("Ok dati caricati");
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.ToString());
            }

        }

        private async Task DeleteRow()
        {
            string query = """
                DELETE
                FROM RecordTelemetria
                DELETE
                FROM Tracciati
                DELETE
                FROM Treni
                """;

            using var connection = new SqlConnection(_connectionString);

            await connection.ExecuteAsync(query);
        }

        private async Task<DateTime?> ParseTimestamp(string? val)
        {
            if (string.IsNullOrWhiteSpace(val) || val == "timestamp_error")
                return null;

            return DateTime.TryParseExact(val.Trim(), TimestampFormats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt) ? dt : null;

            
        }

        private async Task<double?> PulisciVelocita(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            var pulita = System.Text.RegularExpressions.Regex.Replace(val, @"[^0-9.\-]", "");
            return double.TryParse(pulita,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var result) ? result : null;
        }




    }
}
