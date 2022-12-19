﻿using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using TransferManagerCE.Common;
using TransferManagerCE.CustomManager;
using TransferManagerCE.Settings;
using UnifiedUI.Helpers;
using UnityEngine;

namespace TransferManagerCE
{
    internal class SelectionTool : DefaultTool
    {
        public enum SelectionToolMode
        {
            Normal,
            BuildingRestrictionIncoming,
            BuildingRestrictionOutgoing,
        }
        
        public static SelectionTool? Instance = null;
        public SelectionToolMode m_mode = SelectionToolMode.Normal;

        private static bool s_bLoadingTool = false;
        private UIComponent? m_button = null;
        private Color[]? m_color = null;
        private bool m_processedClick = false;
        private HighlightBuildings m_highlightBuildings = new HighlightBuildings();

        public static bool HasUnifiedUIButtonBeenAdded()
        {
            return (Instance != null && Instance.m_button != null);
        }

        public static void AddSelectionTool()
        {
            if (TransferManagerLoader.IsLoaded())
            {
                if (Instance == null)
                {
                    try
                    {
                        s_bLoadingTool = true;
                        Instance = ToolsModifierControl.toolController.gameObject.AddComponent<SelectionTool>();
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Selection tool failed to load: ", e);
                    }
                    finally
                    {
                        s_bLoadingTool = false;
                    }
                }
            } 
            else
            {
                Debug.Log("Game not loaded");
            }
        }

        public static void RemoveUnifiedUITool()
        {
            if (Instance != null)
            {
                if (Instance.m_button != null)
                {
                    UUIHelpers.Destroy(Instance.m_button);
                }
                Instance.Destroy();
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (DependencyUtilities.IsUnifiedUIRunning())
            {
                Texture2D? icon = TextureResources.LoadDllResource("Transfer.png", 32, 32);
                if (icon == null)
                {
                    Debug.Log("Failed to load icon from resources");
                    return;
                }

                m_button = UUIHelpers.RegisterToolButton(
                    name: "TransferManagerCE",
                    groupName: null,
                    tooltip: TransferManagerMain.Title,
                    tool: this,
                    icon: icon,
                    hotkeys: new UUIHotKeys { ActivationKey = ModSettings.SelectionToolHotkey });
            }
        }

        public static void Release() 
        {
            Destroy(FindObjectOfType<SelectionTool>());
        }

        public void SetMode(SelectionToolMode mode)
        {
            m_mode = mode;

            // Update the building panel to the changed state
            if (BuildingPanel.Instance != null && BuildingPanel.Instance.isVisible)
            {
                BuildingPanel.Instance.UpdateTabs();
            }
        }

        public override void SimulationStep()
        {
            base.SimulationStep();

            if (RayCastSegmentAndNode(out var hoveredSegment, out var hoveredNode))
            {
                if (hoveredNode > 0)
                {
                    m_hoverInstance.NetNode = hoveredNode;
                }
            }
        }

        private static bool RayCastSegmentAndNode(out ushort netSegment, out ushort netNode)
        {
            if (RayCastSegmentAndNode(out var output))
            {
                netSegment = output.m_netSegment;
                netNode = output.m_netNode;
                return true;
            }

            netSegment = 0;
            netNode = 0;
            return false;
        }

        private static bool RayCastSegmentAndNode(out RaycastOutput output)
        {
            var input = new RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane)
            {
                m_netService = { m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels },
                m_ignoreSegmentFlags = NetSegment.Flags.None,
                m_ignoreNodeFlags = NetNode.Flags.None,
                m_ignoreTerrain = true,
            };

            return RayCast(input, out output);
        }

        public void Enable()
        {
            if (Instance != null && !Instance.enabled)
            {
                OnEnable();
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            
            // Ensure we are in normal mode.
            m_mode = SelectionToolMode.Normal;

            // We seem to get an eroneous OnEnable call when adding the tool.
            if (!s_bLoadingTool)
            {
                ToolsModifierControl.mainToolbar.CloseEverything();
                ToolsModifierControl.SetTool<SelectionTool>();
                BuildingPanel.Init();
                BuildingPanel.Instance?.ShowPanel();
            }
        }

        public void Disable()
        {
            // Ensure we are in normal mode.
            m_mode = SelectionToolMode.Normal;

            ToolBase oCurrentTool = ToolsModifierControl.toolController.CurrentTool;
            if (oCurrentTool != null && oCurrentTool == Instance && oCurrentTool.enabled)
            {
                OnDisable();
            }

            m_color = null;
        }

        protected override void OnDisable() {
            m_toolController ??= ToolsModifierControl.toolController; // workaround exception in base code.
            base.OnDisable();
            ToolsModifierControl.SetTool<DefaultTool>();
            BuildingPanel.Instance?.HidePanel();
        }

        public void ToogleSelectionTool()
        {
            if (isActiveAndEnabled)
            {
                Disable();
            }
            else
            {
                Enable();
            }
        }
        
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            ToolManager toolManager = Singleton<ToolManager>.instance;

            switch (m_hoverInstance.Type)
            {
                case InstanceType.Building:
                    {
                        base.RenderOverlay(cameraInfo);
                        break;
                    }
                case InstanceType.NetNode:
                    {
                        NetNode oNode = NetManager.instance.m_nodes.m_buffer[m_hoverInstance.NetNode];
                        if (oNode.m_building != 0)
                        {
                            if (BuildingTypeHelper.IsOutsideConnection(oNode.m_building))
                            {
                                RenderManager.instance.OverlayEffect.DrawCircle(
                                    cameraInfo,
                                    GetToolColor(false, false),
                                    oNode.m_position,
                                    oNode.m_bounds.size.magnitude,
                                    oNode.m_position.y - 1f,
                                    oNode.m_position.y + 1f,
                                    true,
                                    true);
                                toolManager.m_drawCallData.m_overlayCalls++;
                            }
                        }
                        break;
                    }
            }

            HighlightSelectedBuildingAndMatches(toolManager, cameraInfo);
            HighlightNodes(cameraInfo);
        }

        private void HighlightSelectedBuildingAndMatches(ToolManager toolManager, RenderManager.CameraInfo cameraInfo)
        {
            // Highlight selected building and all matches
            if (BuildingPanel.Instance != null && BuildingPanel.Instance.GetBuildingId() != 0)
            {
                Building[] BuildingBuffer = BuildingManager.instance.m_buildings.m_buffer;

                ushort usSourceBuildingId = BuildingPanel.Instance.GetBuildingId();
                Building building = BuildingBuffer[usSourceBuildingId];
                if (building.m_flags != 0)
                {
                    // Highlight currently selected building
                    HighlightBuilding(toolManager, BuildingBuffer, usSourceBuildingId, cameraInfo, Color.red);

                    if (m_mode == SelectionToolMode.Normal)
                    {
                        // Now highlight buildings
                        m_highlightBuildings.Highlight(toolManager, BuildingBuffer, cameraInfo);
                    }
                    else
                    {
                        // Building restriction mode.
                        int restrictionId = BuildingPanel.Instance.GetRestrictionId();
                        if (restrictionId != -1)
                        {
                            BuildingSettings settings = BuildingSettingsStorage.GetSettings(usSourceBuildingId);
                            RestrictionSettings restrictions = settings.GetRestrictions(restrictionId);

                            // Seelct appropriate building restrictions
                            HashSet<ushort> buildingRestrictions;
                            if (m_mode == SelectionToolMode.BuildingRestrictionIncoming)
                            {
                                buildingRestrictions = restrictions.m_incomingBuildingsAllowed;
                            }
                            else
                            {
                                buildingRestrictions = restrictions.m_outgoingBuildingsAllowed;
                            }

                            // Now highlight buildings
                            foreach (ushort buildingId in buildingRestrictions)
                            {
                                HighlightBuilding(toolManager, BuildingBuffer, buildingId, cameraInfo, Color.green);
                            }
                        }
                    }
                }
            }
        }

        private void HighlightNodes(RenderManager.CameraInfo cameraInfo)
        {
            int iShowConnection = ModSettings.GetSettings().ShowConnectionGraph;
            if (iShowConnection > 0)
            {
                // DEBUGGING, Show node connection colors
                ConnectedStorage? connectionNodes = null;
                if (iShowConnection == 1)
                {
                    connectionNodes = UnconnectedGraphCache.GetGoodsBufferCopy();
                }
                else if (iShowConnection == 2)
                {
                    connectionNodes = UnconnectedGraphCache.GetServicesBufferCopy();
                }

                if (connectionNodes != null)
                {
                    if (m_color == null || m_color.Length != connectionNodes.Colors)
                    {
                        m_color = new Color[connectionNodes.Colors];
                        for (int i = 0; i < m_color.Length; i++)
                        {
                            m_color[i] = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
                        }
                    }

                    NetNode[] Nodes = Singleton<NetManager>.instance.m_nodes.m_buffer;
                    foreach (KeyValuePair<ushort, int> kvp in connectionNodes)
                    {
                        NetNode oNode = Nodes[kvp.Key];
                        if (kvp.Value < m_color.Length)
                        {
                            RenderManager.instance.OverlayEffect.DrawCircle(
                                        cameraInfo,
                                        m_color[kvp.Value - 1],
                                        oNode.m_position,
                                        oNode.m_bounds.size.magnitude,
                                        oNode.m_position.y - 1f,
                                        oNode.m_position.y + 1f,
                                        true,
                                        true);
                        }
                    }
                }
            }
        }

        public void UpdateSelection()
        {
            if (BuildingPanel.Instance != null && BuildingPanel.Instance.GetBuildingId() != 0)
            {
                m_highlightBuildings.LoadMatches();
            }
        }

        public static void HighlightBuilding(ToolManager toolManager, Building[] BuildingBuffer, ushort usBuildingId, RenderManager.CameraInfo cameraInfo, Color color)
        {
            ref Building building = ref BuildingBuffer[usBuildingId];
            if (building.m_flags != 0)
            {
                // Highlight building
                BuildingTool.RenderOverlay(cameraInfo, ref building, color, color);
                
                // Also highlight any sub buildings
                float m_angle = building.m_angle * 57.29578f;
                BuildingInfo info3 = building.Info;
                if (info3.m_subBuildings != null && info3.m_subBuildings.Length != 0)
                {
                    Matrix4x4 matrix4x = default(Matrix4x4);
                    matrix4x.SetTRS(building.m_position, Quaternion.AngleAxis(m_angle, Vector3.down), Vector3.one);
                    for (int i = 0; i < info3.m_subBuildings.Length; i++)
                    {
                        BuildingInfo buildingInfo = info3.m_subBuildings[i].m_buildingInfo;
                        Vector3 position = matrix4x.MultiplyPoint(info3.m_subBuildings[i].m_position);
                        float angle = (info3.m_subBuildings[i].m_angle + m_angle) * ((float)Math.PI / 180f);
                        buildingInfo.m_buildingAI.RenderBuildOverlay(cameraInfo, color, position, angle, default(Segment3));
                        BuildingTool.RenderOverlay(cameraInfo, buildingInfo, 0, position, angle, color, radius: false);
                    }
                }
            }
        }

        protected override void OnToolGUI(Event e)
        {
            if (m_mode != SelectionToolMode.Normal)
            {
                DrawLabel();
                //TODO m_cursor = 
            }

            if (m_toolController.IsInsideUI)
            {
                base.OnToolGUI(e);
                return;
            }

            if (e.type == EventType.MouseDown && Input.GetMouseButtonDown(0))
            {
                // cancel if the key input was already processed in a previous frame
                if (!m_processedClick)
                {
                    HandleLeftClick();
                    m_processedClick = true;
                }
            }
            else
            {
                m_processedClick = false;
            }
        }

        private void HandleLeftClick()
        {
            if (m_hoverInstance != null)
            {
                switch (m_hoverInstance.Type)
                {
                    case InstanceType.Building:
                        {
                            if (m_mode == SelectionToolMode.Normal)
                            {
                                SelectBuilding(m_hoverInstance.Building);
                            }
                            else
                            {
                                // Building restriction mode.
                                if (BuildingPanel.Instance != null)
                                {
                                    ushort buildingId = BuildingPanel.Instance.GetBuildingId();
                                    if (buildingId != 0 && buildingId != m_hoverInstance.Building)
                                    {
                                        int restrictionId = BuildingPanel.Instance.GetRestrictionId();
                                        if (restrictionId != -1)
                                        {
                                            BuildingSettings settings = BuildingSettingsStorage.GetSettings(buildingId);
                                            RestrictionSettings restrictions = settings.GetRestrictions(restrictionId);
                                            
                                            if (m_mode == SelectionToolMode.BuildingRestrictionIncoming)
                                            {
                                                if (restrictions.m_incomingBuildingsAllowed.Contains(m_hoverInstance.Building))
                                                {
                                                    restrictions.m_incomingBuildingsAllowed.Remove(m_hoverInstance.Building);
                                                }
                                                else
                                                {
                                                    restrictions.m_incomingBuildingsAllowed.Add(m_hoverInstance.Building);
                                                }
                                            }
                                            else
                                            {
                                                if (restrictions.m_outgoingBuildingsAllowed.Contains(m_hoverInstance.Building))
                                                {
                                                    restrictions.m_outgoingBuildingsAllowed.Remove(m_hoverInstance.Building);
                                                }
                                                else
                                                {
                                                    restrictions.m_outgoingBuildingsAllowed.Add(m_hoverInstance.Building);
                                                }
                                            }

                                            // Now update settings
                                            settings.SetRestrictions(restrictionId, restrictions);
                                            BuildingSettingsStorage.SetSettings(buildingId, settings);

                                            // Update tab to reflect selected building
                                            if (BuildingPanel.Instance != null && BuildingPanel.Instance.isVisible)
                                            {
                                                BuildingPanel.Instance.UpdateTabs();
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case InstanceType.NetNode:
                        {
                            NetNode oNode = NetManager.instance.m_nodes.m_buffer[m_hoverInstance.NetNode];
                            if (oNode.m_building != 0)
                            {
                                Building building = BuildingManager.instance.m_buildings.m_buffer[oNode.m_building];
                                if (building.Info?.GetAI() is OutsideConnectionAI)
                                {
                                    SelectBuilding(oNode.m_building);
                                }
                            }
                            break;
                        }
                }
            }
        }

        private void SelectBuilding(ushort buildingId)
        {
            if (!s_bLoadingTool)
            {
                // Open building panel
                BuildingPanel.Instance?.ShowPanel(buildingId);
            }
        }

        private void DrawLabel()
        {
            var text = Localization.Get("btnBuildingRestrictionsSelected");
            var screenPoint = MousePosition;
            var color = GUI.color;
            GUI.color = Color.white;
            DeveloperUI.LabelOutline(new Rect(screenPoint.x, screenPoint.y, 500f, 500f), text, Color.black, Color.cyan, GUI.skin.label, 2f);
            GUI.color = color;
        }

        public static Vector2 MousePosition
        {
            get
            {
                var mouse = Input.mousePosition;
                mouse.y = Screen.height - mouse.y - 20f;
                return mouse;
            }
        }

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();
            if (UIView.library.Get("PauseMenu")?.isVisible == true)
            {
                UIView.library.Hide("PauseMenu");
                ToolsModifierControl.SetTool<DefaultTool>();
            }

            if (Input.GetMouseButtonDown(1))
            {
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }

        protected override void OnDestroy()
        {
            if (m_button != null)
            {
                Destroy(m_button.gameObject);
                m_button = null;
            }

            base.OnDestroy();
        }

        public override NetNode.Flags GetNodeIgnoreFlags() => NetNode.Flags.All;
        public override Building.Flags GetBuildingIgnoreFlags() => Building.Flags.None;
        public override CitizenInstance.Flags GetCitizenIgnoreFlags() => CitizenInstance.Flags.All;
        public override DisasterData.Flags GetDisasterIgnoreFlags() => DisasterData.Flags.All;
        public override District.Flags GetDistrictIgnoreFlags() => District.Flags.All;
        public override TransportLine.Flags GetTransportIgnoreFlags() => TransportLine.Flags.None;
        public override VehicleParked.Flags GetParkedVehicleIgnoreFlags() => VehicleParked.Flags.All;
        public override TreeInstance.Flags GetTreeIgnoreFlags() => TreeInstance.Flags.All;
        public override PropInstance.Flags GetPropIgnoreFlags() => PropInstance.Flags.All;
        //public override Vehicle.Flags GetVehicleIgnoreFlags() => Vehicle.Flags.All;

        public override NetSegment.Flags GetSegmentIgnoreFlags(out bool nameOnly)
        {
            nameOnly = false;
            return NetSegment.Flags.All;
        }

        protected override bool CheckNode(ushort node, ref ToolErrors errors) => true;
        protected override bool CheckSegment(ushort segment, ref ToolErrors errors) => true;
        protected override bool CheckBuilding(ushort building, ref ToolErrors errors) => true;
        protected override bool CheckProp(ushort prop, ref ToolErrors errors) => true;
        protected override bool CheckTree(uint tree, ref ToolErrors errors) => true;
        protected override bool CheckVehicle(ushort vehicle, ref ToolErrors errors) => true;
        protected override bool CheckParkedVehicle(ushort parkedVehicle, ref ToolErrors errors) => true;
        protected override bool CheckCitizen(ushort citizenInstance, ref ToolErrors errors) => true;
        protected override bool CheckDisaster(ushort disaster, ref ToolErrors errors) => true;
    }
}