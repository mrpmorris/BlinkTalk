namespace Holovis.QuaternionFiltering
{
    public class KalmanFilter
    {
        float ProcessNoise; //Standard deviation - Q
        float MeasurementNoise; // R
        float EstimationConfidence; //P
        float EstimatedValue; // X 
        float Gain; // K

        public KalmanFilter(float initialValue = 0f, float confidenceOfInitialValue = 1f, float processNoise = 0.0001f, float measurementNoise = 0.01f)
        {
            this.ProcessNoise = processNoise;
            this.MeasurementNoise = measurementNoise;
            this.EstimationConfidence = confidenceOfInitialValue;
            this.EstimatedValue = initialValue;
        }

        public float Update(float measurement)
        {
            Gain = (EstimationConfidence + ProcessNoise) / (EstimationConfidence + ProcessNoise + MeasurementNoise);
            EstimationConfidence = MeasurementNoise * (EstimationConfidence + ProcessNoise) / (MeasurementNoise + EstimationConfidence + ProcessNoise);
            float result = EstimatedValue + (measurement - EstimatedValue) * Gain;
            EstimatedValue = result;
            return result;
        }
    }

}
