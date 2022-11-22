using BaseX;
using FrooxEngine;
using System;
using System.Numerics;
using static QuestProModule.ALXR.ALXRModule;

namespace QuestProModule
{
    public class EyeDevice : IInputDriver
    {
        private Eyes _eyes;
        InputInterface input;
        public int UpdateOrder => 100;
        private float _openExponent = 1.0f;
        public EyeDevice(float openExponent)
        {
            Engine.Current.OnShutdown += Teardown;
            _openExponent = openExponent;
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
            input = inputInterface;
        }

        public void UpdateInputs(float deltaTime)
        {
            _eyes.IsEyeTrackingActive = input.VR_Active;

            _eyes.LeftEye.IsTracking = input.VR_Active;

            QuestProMod.qpm.GetEyeExpressions(FBEye.Left, _eyes.LeftEye);
            QuestProMod.qpm.GetEyeExpressions(FBEye.Right, _eyes.RightEye);
            QuestProMod.qpm.GetEyeExpressions(FBEye.Combined, _eyes.CombinedEye);

            _eyes.LeftEye.Openness = MathX.Pow(_eyes.LeftEye.Openness, _openExponent);
            _eyes.RightEye.Openness = MathX.Pow(_eyes.RightEye.Openness, _openExponent);
            _eyes.CombinedEye.Openness = MathX.Pow(_eyes.CombinedEye.Openness, _openExponent);

            _eyes.ComputeCombinedEyeParameters();
            _eyes.ConvergenceDistance = 0f;
            _eyes.Timestamp += deltaTime;
            _eyes.FinishUpdate();
        }
    }
}
