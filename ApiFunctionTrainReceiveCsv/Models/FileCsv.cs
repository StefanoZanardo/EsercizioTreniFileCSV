using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApiFunctionTrainReceiveCsv.Models
{
    public class FileCsv
    {

        public string NameFile { get; set; }

        public Stream content { get; set; }
    }

public class RecordTelemetria
    {
        [Name("record_id")]
        public string? RecordId { get; set; }

        [Name("timestamp")]
        public string? Timestamp { get; set; }

        [Name("train_id")]
        public string? TrainId { get; set; }

        [Name("nome_treno")]
        public string? NomeTreno { get; set; }

        [Name("linea")]
        public string? Linea { get; set; }

        [Name("track_id")]
        public string? TrackId { get; set; }

        [Name("tratta")]
        public string? Tratta { get; set; }

        [Name("stazione_riferimento")]
        public string? StazioneRiferimento { get; set; }

        [Name("latitudine")]
        [Optional]
        public double? Latitudine { get; set; }

        [Name("longitudine")]
        [Optional]
        public double? Longitudine { get; set; }

        [Name("velocita_kmh")]
        public string? VelocitaRaw { get; set; } // string perché può avere "53.5 kmh"

        [Name("accelerazione_ms2")]
        [Optional]
        public double? AccelerazioneMs2 { get; set; }

        [Name("temperatura_motore_c")]
        [Optional]
        public double? TemperaturaMotoreC { get; set; }

        [Name("pressione_freni_bar")]
        [Optional]
        public double? PressioneFreniBar { get; set; }

        [Name("vibrazione_mm_s")]
        [Optional]
        public double? VibrazioneMmS { get; set; }

        [Name("deformazione_binario_mm")]
        [Optional]
        public double? DeformazioneBinarioMm { get; set; }

        [Name("temperatura_rotaia_c")]
        [Optional]
        public double? TemperaturaRotaiaC { get; set; }

        [Name("tipo_evento")]
        public string? TipoEvento { get; set; }

        [Name("ritardo_minuti")]
        [Optional]
        public int? RitardoMinuti { get; set; }

        [Name("livello_severita")]
        public string? LivelloSeverita { get; set; }

        [Name("anomaly_score")]
        [Optional]
        public double? AnomalyScore { get; set; }

        [Name("fonte_dato")]
        public string? FonteDato { get; set; }

        [Ignore]
        public double? VelocitaKmh { get; set; }

        [Ignore]
        public DateTime? TimestampParsed { get; set; }





    }
}
