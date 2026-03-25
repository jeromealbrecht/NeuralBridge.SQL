using NAudio.Dsp;
using NAudio.Wave;
using System.Text.Json;

namespace NeuralBridge.SQL.Services;

public record SpectralResult(
    float LowEnergy,    // 20Hz   - 250Hz  (dBFS)
    float MidEnergy,    // 250Hz  - 4kHz   (dBFS)
    float HighEnergy,   // 4kHz   - 10kHz  (dBFS) — cible De-esser
    float AirEnergy,    // 10kHz  - 20kHz  (dBFS)
    float HighPeakHz,   // Fréquence du pic dominant dans la bande high (Hz)
    string FftDataJson  // Snapshot complet : magnitudes en dBFS par bin (JSON)
);

public class SpectralAnalyzer
{
    private const int FftLength = 1024;

    public static SpectralResult Analyze(string filePath)
    {
        using var reader = new AudioFileReader(filePath);
        var buffer = new float[FftLength];
        int sampleRate = reader.WaveFormat.SampleRate;

        float lowAccum = 0, midAccum = 0, highAccum = 0, airAccum = 0;
        var binMagnitudes = new float[FftLength / 2];
        int count = 0;
        float hzPerBin = (float)sampleRate / FftLength;

        while (reader.Read(buffer, 0, FftLength) > 0)
        {
            var fftComplex = new Complex[FftLength];
            for (int i = 0; i < FftLength; i++)
            {
                float window = (float)(0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (FftLength - 1)));
                fftComplex[i].X = buffer[i] * window;
                fftComplex[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(FftLength, 2), fftComplex);

            lowAccum  += CalculateBandEnergy(fftComplex, 20,    250,   sampleRate);
            midAccum  += CalculateBandEnergy(fftComplex, 250,   4000,  sampleRate);
            highAccum += CalculateBandEnergy(fftComplex, 4000,  10000, sampleRate);
            airAccum  += CalculateBandEnergy(fftComplex, 10000, 20000, sampleRate);

            for (int i = 0; i < FftLength / 2; i++)
            {
                float mag = (float)Math.Sqrt(fftComplex[i].X * fftComplex[i].X + fftComplex[i].Y * fftComplex[i].Y);
                binMagnitudes[i] += mag;
            }

            count++;
        }

        if (count == 0)
            throw new InvalidDataException("Le fichier audio est vide ou illisible.");

        var binDb = new float[FftLength / 2];
        for (int i = 0; i < binMagnitudes.Length; i++)
            binDb[i] = ToDb(binMagnitudes[i] / count);

        string fftJson = JsonSerializer.Serialize(binDb);

        int highBinMin = Math.Clamp((int)(4000 / hzPerBin), 0, FftLength / 2 - 1);
        int highBinMax = Math.Clamp((int)(10000 / hzPerBin), 0, FftLength / 2 - 1);
        int peakBin = highBinMin;
        for (int i = highBinMin + 1; i <= highBinMax; i++)
            if (binMagnitudes[i] > binMagnitudes[peakBin])
                peakBin = i;
        float highPeakHz = peakBin * hzPerBin;

        return new SpectralResult(
            LowEnergy:   ToDb(lowAccum  / count),
            MidEnergy:   ToDb(midAccum  / count),
            HighEnergy:  ToDb(highAccum / count),
            AirEnergy:   ToDb(airAccum  / count),
            HighPeakHz:  highPeakHz,
            FftDataJson: fftJson
        );
    }

    private static float CalculateBandEnergy(Complex[] fftResult, int freqMin, int freqMax, int sampleRate)
    {
        float hzPerBin = (float)sampleRate / FftLength;
        int binMin = Math.Clamp((int)(freqMin / hzPerBin), 0, FftLength / 2 - 1);
        int binMax = Math.Clamp((int)(freqMax / hzPerBin), 0, FftLength / 2 - 1);

        float energy = 0;
        for (int i = binMin; i <= binMax; i++)
        {
            float magnitude = (float)Math.Sqrt(fftResult[i].X * fftResult[i].X + fftResult[i].Y * fftResult[i].Y);
            energy += magnitude * magnitude;
        }

        return energy / (binMax - binMin + 1);
    }

    // 10*log10(magnitude²) == 20*log10(magnitude) — plancher à -120 dBFS pour éviter log(0)
    private static float ToDb(float linearPower) =>
        linearPower > 0 ? (float)(10.0 * Math.Log10(linearPower)) : -120f;
}
