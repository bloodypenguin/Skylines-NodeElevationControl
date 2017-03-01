using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace NodeElevationControl
{
    public class NodeElevationControl : MonoBehaviour
    {
        private SavedInputKey m_buildElevationUp;
        private SavedInputKey m_buildElevationDown;

        public void Awake()
        {
            this.m_buildElevationUp = new SavedInputKey(Settings.buildElevationUp, Settings.gameSettingsFile, DefaultSettings.buildElevationUp, true);
            this.m_buildElevationDown = new SavedInputKey(Settings.buildElevationDown, Settings.gameSettingsFile, DefaultSettings.buildElevationDown, true);
            UIInput.eventProcessKeyEvent += new UIInput.ProcessKeyEventHandler(this.ProcessKeyEvent);
        }

        public void Update()
        {
            if ((Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift)) && Input.GetKeyDown(KeyCode.N))
            {
                var tool = ToolsModifierControl.GetTool<NodeElevationTool>();
                if (tool == null)
                {
                    return;
                }
                tool.enabled = true;
            }
        }

        private void ProcessKeyEvent(EventType eventType, KeyCode keyCode, EventModifiers modifiers)
        {
            if (eventType != EventType.KeyDown)
                return;

            var tool = ToolsModifierControl.GetCurrentTool<NodeElevationTool>();
            if (tool == null)
            {
                return;
            }
            var node = tool.m_hoveredNode;
            if (node == 0)
            {
                return;
            }
            var position = NetManager.instance.m_nodes.m_buffer[node].m_position;
            if (this.m_buildElevationUp.IsPressed(eventType, keyCode, modifiers))
            {
                NetManager.instance.m_nodes.m_buffer[node].m_position = new Vector3(position.x, position.y + 0.1f, position.z);
                NetManager.instance.UpdateNode(node, 0, 0);
            }
            else if (this.m_buildElevationDown.IsPressed(eventType, keyCode, modifiers))
            {
                NetManager.instance.m_nodes.m_buffer[node].m_position = new Vector3(position.x, position.y - 0.1f, position.z);
                NetManager.instance.UpdateNode(node, 0, 0);
            }
        }

    }
}