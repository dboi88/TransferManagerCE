using ColossalFramework.UI;
using UnityEngine;

namespace TransferManagerCE
{
    public class InfoPanelButtons
    {
        private static UIButton? m_button = null;

        internal static void AddInfoPanelButtons()
        {
            // Zoned building (PrivateBuilding) info panel.
            AddInfoPanelButton(UIView.library.Get<ZonedBuildingWorldInfoPanel>(typeof(ZonedBuildingWorldInfoPanel).Name));

            // Service building (PlayerBuilding) info panel.
            AddInfoPanelButton(UIView.library.Get<CityServiceWorldInfoPanel>(typeof(CityServiceWorldInfoPanel).Name));

            // Service building (PlayerBuilding) info panel.
            AddInfoPanelButton(UIView.library.Get<UniqueFactoryWorldInfoPanel>(typeof(UniqueFactoryWorldInfoPanel).Name));

            // Warehouse building
            AddInfoPanelButton(UIView.library.Get<WarehouseWorldInfoPanel>(typeof(WarehouseWorldInfoPanel).Name));

            // Warehouse building
            AddInfoPanelButton(UIView.library.Get<ShelterWorldInfoPanel>(typeof(ShelterWorldInfoPanel).Name));
        }

        private static void AddInfoPanelButton(BuildingWorldInfoPanel infoPanel)
        {
            UIComponent problemsPanel;
            float relativeY = 40f;

            // Player info panels have wrappers, zoned ones don't.
            UIComponent wrapper = infoPanel.Find("Wrapper");
            if (wrapper == null)
            {
                problemsPanel = infoPanel.Find("ProblemsPanel");
            }
            else
            {
                problemsPanel = wrapper.Find("ProblemsPanel");
            }

            try
            {
                // Position button vertically in the middle of the problems panel.  If wrapper panel exists, we need to add its offset as well.
                relativeY = (wrapper == null ? 0 : wrapper.relativePosition.y) + problemsPanel.relativePosition.y + ((problemsPanel.height - 34) / 2);
            }
            catch
            {
                // Don't really care; just use default relative Y.
                Debug.Log("couldn't find ProblemsPanel relative position");
            }

            m_button = infoPanel.component.AddUIComponent<UIButton>();
            if (m_button != null)
            {
                // Basic button setup.
                m_button.size = new Vector2(34, 34);
                m_button.normalBgSprite = "ToolbarIconGroup6Normal";
                m_button.normalFgSprite = "Transfer";
                m_button.focusedBgSprite = "ToolbarIconGroup6Focused";
                m_button.hoveredBgSprite = "ToolbarIconGroup6Hovered";
                m_button.pressedBgSprite = "ToolbarIconGroup6Pressed";
                m_button.disabledBgSprite = "ToolbarIconGroup6Disabled";
                m_button.name = "TransferManagerCEButton";
                m_button.tooltip = "Open Transfer Manager CE";
                m_button.atlas = TransferManagerLoader.LoadResources();
         
                // Buttons to avoid
                // RICO = 5f
                // Repainter = 42f
                // Advanced building control = 62f
                float fXOffset = -5f;
                if (infoPanel is ZonedBuildingWorldInfoPanel || infoPanel is CityServiceWorldInfoPanel)
                {
                    if (DependencyUtilities.IsPloppableRICORunning())
                    {
                        fXOffset += -m_button.width;

                        if (DependencyUtilities.IsRepainterRunning())
                        {
                            fXOffset += -m_button.width + 4f; // Button not as big move it back a bit

                            if (infoPanel is ZonedBuildingWorldInfoPanel && DependencyUtilities.IsAdvancedBuildingLevelRunning())
                            {
                                // Need to shift past all 3 buttons
                                fXOffset += -m_button.width + 10f; // Some of the other icons arent as big, pull it back a bit
                            }
                        }
                    }
                }

                m_button.AlignTo(infoPanel.component, UIAlignAnchor.TopRight);
                m_button.relativePosition += new Vector3(fXOffset, relativeY, 0f);

                // Event handler.
                m_button.eventClick += (control, clickEvent) =>
                {
                    BuildingPanel.Init();

                    // Select current building in the building details panel and show.
                    if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                    {
                        // Open building panel
                        BuildingPanel.Instance?.SetPanelBuilding(WorldInfoPanel.GetCurrentInstanceID().Building);
                    }

                    WorldInfoPanel.HideAllWorldInfoPanels();

                    if (SelectionTool.Instance == null)
                    {
                        SelectionTool.AddSelectionTool();
                    }
                    if (SelectionTool.Instance != null)
                    {
                        SelectionTool.Instance.Enable();
                    }
                    else
                    {
                        Debug.Log("Selection tool is null");
                        BuildingPanel.Instance?.ShowPanel();
                    }
                };
            }
        }
    }
}
