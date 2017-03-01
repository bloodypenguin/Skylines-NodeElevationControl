using System.Collections.Generic;
using System.Linq;
using ColossalFramework.Math;
using ColossalFramework.UI;
using UnityEngine;

namespace NodeElevationControl
{
    class NodeElevationTool : ToolBase
    {
        const NetNode.Flags CUSTOMIZED_NODE_FLAG = (NetNode.Flags) (1 << 28);

        class NodeLaneMarker
        {
            public ushort m_node;
            public Vector3 m_position;
            public bool m_isSource;
            public uint m_lane;
            public float m_size = 1f;
            public Color m_color;
            public FastList<NodeLaneMarker> m_connections = new FastList<NodeLaneMarker>();
        }

        class SegmentLaneMarker
        {
            public uint m_lane;
            public int m_laneIndex;
            public float m_size = 1f;
            public Bezier3 m_bezier;
            public Bounds[] m_bounds;
       }

        struct Segment
        {
            public ushort m_segmentId;
            public ushort m_targetNode;
        }

        ushort m_hoveredSegment;
        public ushort m_hoveredNode;

        Dictionary<ushort, Segment> m_segments = new Dictionary<ushort, Segment>();
        
        List<SegmentLaneMarker> m_selectedLaneMarkers = new List<SegmentLaneMarker>();
        int m_hoveredLanes;
        UIButton m_toolButton;

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (m_toolController.IsInsideUI)
                return;

            if (!RayCastSegmentAndNode(out m_hoveredSegment, out m_hoveredNode))
            {
                // clear lanes
                if (Input.GetMouseButtonUp(1))
                {
                    m_selectedLaneMarkers.Clear();
                    if (OnEndLaneCustomization != null)
                        OnEndLaneCustomization();
                }

                m_segments.Clear();
                return;
            }


            if (m_hoveredSegment != 0)
            {
                NetSegment segment = NetManager.instance.m_segments.m_buffer[m_hoveredSegment];
                NetNode startNode = NetManager.instance.m_nodes.m_buffer[segment.m_startNode];
                NetNode endNode = NetManager.instance.m_nodes.m_buffer[segment.m_endNode];
                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (startNode.CountSegments() > 0)
                {
                    Bounds bounds = startNode.m_bounds;
                    if (m_hoveredNode != 0)
                        bounds.extents /= 2f;
                    if (bounds.IntersectRay(mouseRay))
                    {
                        m_hoveredSegment = 0;
                        m_hoveredNode = segment.m_startNode;
                    }
                }

                if (m_hoveredSegment != 0 && endNode.CountSegments() > 0)
                {
                    Bounds bounds = endNode.m_bounds;
                    if (m_hoveredNode != 0)
                        bounds.extents /= 2f;
                    if (bounds.IntersectRay(mouseRay))
                    {
                        m_hoveredSegment = 0;
                        m_hoveredNode = segment.m_endNode;
                    }
                }

                if (m_hoveredSegment != 0)
                {
                    m_hoveredNode = 0;
                    if (!m_segments.ContainsKey(m_hoveredSegment))
                    {
                        m_segments.Clear();
                        SetSegments(m_hoveredSegment);
                        //TODO(earalov): uncomment
                        //SetLaneMarkers();
                    }
                }
//TODO(earalov): uncomment
//                else if (Input.GetMouseButtonUp(1))
//                {
//                    // clear lane selection
//                    m_selectedLaneMarkers.Clear();
//                    if (OnEndLaneCustomization != null)
//                        OnEndLaneCustomization();
//                }

            }
            else if (m_hoveredNode != 0 && NetManager.instance.m_nodes.m_buffer[m_hoveredNode].CountSegments() < 2)
            {
                m_hoveredNode = 0;
            }

            if (m_hoveredSegment == 0)
            {
                m_segments.Clear();
            }

            //TODO(earalov): uncomment
            //            if (Input.GetMouseButtonUp(0))
            //            {
            //                m_selectedNode = m_hoveredNode;
            //                m_hoveredNode = 0;
            //
            //                if (m_selectedNode != 0)
            //                    SetNodeMarkers(m_selectedNode, true);
            //            }


            if (Input.GetMouseButtonUp(1))
            {
                m_segments.Clear();
                m_hoveredNode = 0;
                ToolsModifierControl.SetTool<DefaultTool>();
            }

        }

        float time = 0;

        protected override void OnEnable()
        {
            base.OnEnable();

            // hack to stop bug that disables and enables this tool the first time the panel is clicked
            if (Time.realtimeSinceStartup - time < 0.2f)
            {
                time = 0;
                return;
            }

            m_hoveredNode = m_hoveredSegment = 0;
            m_selectedLaneMarkers.Clear();
            m_segments.Clear();
            if (OnEndLaneCustomization != null)
                OnEndLaneCustomization();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            time = Time.realtimeSinceStartup;
            //m_selectedLaneMarkers.Clear();
            //if (OnEndLaneCustomization != null)
            //	OnEndLaneCustomization();
        }
        
        void SetSegments(ushort segmentId)
        {
            NetSegment segment = NetManager.instance.m_segments.m_buffer[segmentId];
            Segment seg = new Segment()
            {
                m_segmentId = segmentId,
                m_targetNode = segment.m_endNode
            };

            m_segments[segmentId] = seg;

            ushort infoIndex = segment.m_infoIndex;
            NetNode node = NetManager.instance.m_nodes.m_buffer[segment.m_startNode];
            if (node.CountSegments() == 2)
                SetSegments(node.m_segment0 == segmentId ? node.m_segment1 : node.m_segment0, infoIndex, ref seg);

            node = NetManager.instance.m_nodes.m_buffer[segment.m_endNode];
            if (node.CountSegments() == 2)
                SetSegments(node.m_segment0 == segmentId ? node.m_segment1 : node.m_segment0, infoIndex, ref seg);
        }

        void SetSegments(ushort segmentId, ushort infoIndex, ref Segment previousSeg)
        {
            NetSegment segment = NetManager.instance.m_segments.m_buffer[segmentId];

            if (segment.m_infoIndex != infoIndex || m_segments.ContainsKey(segmentId))
                return;

            Segment seg = default(Segment);
            seg.m_segmentId = segmentId;

            NetSegment previousSegment = NetManager.instance.m_segments.m_buffer[previousSeg.m_segmentId];
            ushort nextNode;
            if ((segment.m_startNode == previousSegment.m_endNode) ||
                (segment.m_startNode == previousSegment.m_startNode))
            {
                nextNode = segment.m_endNode;
                seg.m_targetNode = segment.m_startNode == previousSeg.m_targetNode
                    ? segment.m_endNode
                    : segment.m_startNode;
            }
            else
            {
                nextNode = segment.m_startNode;
                seg.m_targetNode = segment.m_endNode == previousSeg.m_targetNode
                    ? segment.m_startNode
                    : segment.m_endNode;
            }

            m_segments[segmentId] = seg;

            NetNode node = NetManager.instance.m_nodes.m_buffer[nextNode];
            if (node.CountSegments() == 2)
                SetSegments(node.m_segment0 == segmentId ? node.m_segment1 : node.m_segment0, infoIndex, ref seg);
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);
            if (m_hoveredNode != 0)
            {
                NetNode node = NetManager.instance.m_nodes.m_buffer[m_hoveredNode];
                RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, new Color(1f, 1f, 1f, 0.75f),
                    node.m_position, 15f, node.m_position.y - 1f, node.m_position.y + 1f, true, true);
            }
        }

        bool RayCastSegmentAndNode(out RaycastOutput output)
        {
            RaycastInput input = new RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition),
                Camera.main.farClipPlane);
            //input.m_netService.m_service = ItemClass.Service.Road;
            input.m_netService.m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels;
            input.m_ignoreSegmentFlags = NetSegment.Flags.None;
            input.m_ignoreNodeFlags = NetNode.Flags.None;
            input.m_ignoreTerrain = true;

            return RayCast(input, out output);
        }

        bool RayCastSegmentAndNode(out ushort netSegment, out ushort netNode)
        {
            RaycastOutput output;
            if (RayCastSegmentAndNode(out output))
            {
                netSegment = output.m_netSegment;
                netNode = output.m_netNode;
                return true;
            }

            netSegment = 0;
            netNode = 0;
            return false;
        }


        public event System.Action OnStartLaneCustomization;
        public event System.Action OnEndLaneCustomization;
    }
}

