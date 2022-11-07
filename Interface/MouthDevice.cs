using BaseX;
using FrooxEngine;

namespace NeosEyeFaceAPI
{
    public class MouthDevice : IInputDriver
    {
        private Mouth _mouth;
        public int UpdateOrder => 100;

        public MouthDevice()
        {
            Engine.Current.OnShutdown += Teardown;
        }

        private void Teardown()
        {

        }

        public void CollectDeviceInfos(DataTreeList list)
        {
            var mouthDataTreeDictionary = new DataTreeDictionary();
            mouthDataTreeDictionary.Add("Name", "Generic Face Tracking");
            mouthDataTreeDictionary.Add("Type", "Face Tracking");
            mouthDataTreeDictionary.Add("Model", "Generic Face Model");
            list.Add(mouthDataTreeDictionary);
        }

        public void RegisterInputs(InputInterface inputInterface)
        {
            _mouth = new Mouth(inputInterface, "Generic Mouth Tracking");
        }

        public void UpdateInputs(float deltaTime)
        {
            _mouth.IsTracking = true;

            _mouth.Jaw = float3.Zero;
            _mouth.Tongue = float3.Zero;

            _mouth.JawOpen = 0f;
            _mouth.MouthPout = 0f;
            _mouth.TongueRoll = 0f;

            _mouth.LipBottomOverUnder = 0f;
            _mouth.LipBottomOverturn = 0f;
            _mouth.LipTopOverUnder = 0f;
            _mouth.LipTopOverturn = 0f;

            _mouth.LipLowerHorizontal = 0f;
            _mouth.LipUpperHorizontal = 0f;

            _mouth.LipLowerLeftRaise = 0f;
            _mouth.LipLowerRightRaise = 0f;
            _mouth.LipUpperRightRaise = 0f;
            _mouth.LipUpperLeftRaise = 0f;

            _mouth.MouthRightSmileFrown = 0f;
            _mouth.MouthLeftSmileFrown = 0f;
            _mouth.CheekLeftPuffSuck = 0f;
            _mouth.CheekRightPuffSuck = 0f;
        }
    }
}
