using BaseX;
using FrooxEngine;

namespace QuestProModule
{
    public class MouthDevice : IInputDriver
    {
        private Mouth mouth;
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
            mouthDataTreeDictionary.Add("Name", "Quest Pro Face Tracking");
            mouthDataTreeDictionary.Add("Type", "Face Tracking");
            mouthDataTreeDictionary.Add("Model", "Quest Pro");
            list.Add(mouthDataTreeDictionary);
        }

        public void RegisterInputs(InputInterface inputInterface)
        {
            mouth = new Mouth(inputInterface, "Quest Pro Mouth Tracking");
        }

        public void UpdateInputs(float deltaTime)
        {
            QuestProMod.qpm.GetFacialExpressions(mouth);
        }
    }
}
