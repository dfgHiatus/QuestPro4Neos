using BaseX;
using FrooxEngine;
using static QuestProModule.ALXR.ALXRModule;

namespace QuestProModule
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
            eyeDataTreeDictionary.Add("Name", "Quest Pro Eye Tracking");
            eyeDataTreeDictionary.Add("Type", "Eye Tracking");
            eyeDataTreeDictionary.Add("Model", "Quest Pro");
            list.Add(eyeDataTreeDictionary);
        }

        public void RegisterInputs(InputInterface inputInterface)
        {
            _eyes = new Eyes(inputInterface, "Quest Pro Eye Tracking");
        }

        public void UpdateInputs(float deltaTime)
        {
            _eyes.IsEyeTrackingActive = Engine.Current.InputInterface.VR_Active;
            _eyes.IsTracking = Engine.Current.InputInterface.VR_Active;

            QuestProMod.qpm.GetEyeExpressions(FBEye.Left, in _eyes.LeftEye);
            QuestProMod.qpm.GetEyeExpressions(FBEye.Right, in _eyes.RightEye);
            QuestProMod.qpm.GetEyeExpressions(FBEye.Combined, in _eyes.CombinedEye);
            
            _eyes.ComputeCombinedEyeParameters();
            _eyes.ConvergenceDistance = 0f;
            _eyes.Timestamp += deltaTime;
            _eyes.FinishUpdate();
        }
    }
}
