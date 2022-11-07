using BaseX;
using FrooxEngine;

namespace NeosEyeFaceAPI
{
    public class EyeDevice : IInputDriver
    {
        private Eyes _eyes;
        public int UpdateOrder => 100;

        public EyeDevice()
        {
            Engine.Current.OnShutdown += Teardown;
        }

        private void Teardown()
        {
            
        }

        public void CollectDeviceInfos(DataTreeList list)
        {
            var eyeDataTreeDictionary = new DataTreeDictionary();
            eyeDataTreeDictionary.Add("Name", "Generic Eye Tracking");
            eyeDataTreeDictionary.Add("Type", "Eye Tracking");
            eyeDataTreeDictionary.Add("Model", "Generic Eye Model");
            list.Add(eyeDataTreeDictionary);
        }

        public void RegisterInputs(InputInterface inputInterface)
        {
            _eyes = new Eyes(inputInterface, "Generic Eye Tracking");
        }

        public void UpdateInputs(float deltaTime)
        {
            _eyes.IsEyeTrackingActive = _eyes.IsEyeTrackingActive;

            UpdateEye(float3.Zero, float3.Zero, true, 0.003f, 1f, 
                0f, 0f, 0f, deltaTime, _eyes.LeftEye);
            UpdateEye(float3.Zero, float3.Zero, true, 0.003f, 1f, 
                0f, 0f, 0f, deltaTime, _eyes.RightEye);

            UpdateEye(float3.Zero, float3.Zero, true, 0.003f, 1f, 
                0f, 0f, 0f, deltaTime, _eyes.CombinedEye);
            _eyes.ComputeCombinedEyeParameters();

            _eyes.ConvergenceDistance = 0f;
            _eyes.Timestamp += deltaTime;
            _eyes.FinishUpdate();
        }

        private void UpdateEye(float3 gazeDirection, float3 gazeOrigin, bool status, float pupilSize, float openness,
            float widen, float squeeze, float frown, float deltaTime, Eye eye)
        {
            eye.IsDeviceActive = Engine.Current.InputInterface.VR_Active;
            eye.IsTracking = status;

            if (eye.IsTracking)
            {
                eye.UpdateWithDirection(gazeDirection);
                eye.RawPosition = gazeOrigin;
                eye.PupilDiameter = pupilSize;
            }

            eye.Openness = openness;
            eye.Widen = widen;
            eye.Squeeze = squeeze;
            eye.Frown = frown;
        }
    }
}
