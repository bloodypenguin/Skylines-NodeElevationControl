using System.Reflection;
using ICities;
using UnityEngine;

namespace NodeElevationControl
{
    public class LoadingExtension : LoadingExtensionBase
    {

        static readonly string[] sm_collectionPrefixes = new string[] { "", "Europe ", "Winter " };

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            var toolController = TryGetComponent<ToolController>("Tool Controller");
            if (toolController == null)
                return;
            AddTool<NodeElevationTool>(toolController);
            ToolsModifierControl.SetTool<DefaultTool>();

            var go = new GameObject("NodeElevationControl");
            go.AddComponent<NodeElevationControl>();
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            var go = GameObject.Find("NodeElevationControl");
            if (go != null)
            {
                GameObject.Destroy(go);
            }
        }


        void AddTool<T>(ToolController toolController) where T : ToolBase
        {
            if (toolController.GetComponent<T>() != null)
                return;

            toolController.gameObject.AddComponent<T>();

            // contributed by Japa
            FieldInfo toolControllerField = typeof(ToolController).GetField("m_tools", BindingFlags.Instance | BindingFlags.NonPublic);
            if (toolControllerField != null)
                toolControllerField.SetValue(toolController, toolController.GetComponents<ToolBase>());
            FieldInfo toolModifierDictionary = typeof(ToolsModifierControl).GetField("m_Tools", BindingFlags.Static | BindingFlags.NonPublic);
            if (toolModifierDictionary != null)
                toolModifierDictionary.SetValue(null, null); // to force a refresh
        }

        T TryGetComponent<T>(string name)
        {
            foreach (string prefix in sm_collectionPrefixes)
            {
                GameObject go = GameObject.Find(prefix + name);
                if (go != null)
                    return go.GetComponent<T>();
            }

            return default(T);
        }



    }
}