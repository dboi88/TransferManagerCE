using System;
using System.Collections.Generic;
using static TransferManager;
using static TransferManagerCE.BuildingTypeHelper;

namespace TransferManagerCE.Data
{
    public class StatusDataProcessingFacility : StatusData
    {
        public StatusDataProcessingFacility(TransferReason reason, BuildingType eBuildingType, ushort BuildingId, ushort responder, ushort target) :
            base(reason, eBuildingType, BuildingId, responder, target)
        {
        }

        public override string GetValue()
        {
            Building building = BuildingManager.instance.m_buildings.m_buffer[m_buildingId];
            ProcessingFacilityAI? buildingAI = building.Info?.m_buildingAI as ProcessingFacilityAI;
            if (buildingAI != null)
            {
                double dValue = 0;
                double dBufferSize = 0;
                if (buildingAI.m_inputResource1 == m_material)
                {
                    dValue = (building.m_customBuffer2 * 0.001);
                    dBufferSize = buildingAI.GetInputBufferSize1(m_buildingId, ref building) * 0.001;
                } 
                else if (buildingAI.m_inputResource2 == m_material)
                {
                    dValue = ((building.m_teens << 8) | building.m_youngs) * 0.001;
                    dBufferSize = buildingAI.GetInputBufferSize2(m_buildingId, ref building) * 0.001;
                }
                else if (buildingAI.m_inputResource3 == m_material)
                {
                    dValue = ((building.m_adults << 8) | building.m_seniors) * 0.001;
                    dBufferSize = buildingAI.GetInputBufferSize3(m_buildingId, ref building) * 0.001;
                }
                else if (buildingAI.m_inputResource4 == m_material)
                {
                    dValue = ((building.m_education1 << 8) | building.m_education2) * 0.001;
                    dBufferSize = buildingAI.GetInputBufferSize4(m_buildingId, ref building) * 0.001;
                }
                else if (m_material == buildingAI.m_outputResource)
                {
                    dValue = building.m_customBuffer1 * 0.001;
                    dBufferSize = buildingAI.GetOutputBufferSize(m_buildingId, ref building) * 0.001;
                }
                return Math.Round(dValue).ToString("N0") + "/" + Math.Round(dBufferSize).ToString("N0");
            }
            return 0.ToString();
        }

        public override string GetTimer()
        {
            string sTimer = base.GetTimer();

            Building building = BuildingManager.instance.m_buildings.m_buffer[m_buildingId];
            if (building.m_flags != 0)
            {
                ProcessingFacilityAI? buildingAI = building.Info?.m_buildingAI as ProcessingFacilityAI;
                if (buildingAI != null)
                {
                    if (m_material == buildingAI.m_outputResource)
                    {
                        if (building.m_outgoingProblemTimer > 0)
                        {
                            if (string.IsNullOrEmpty(sTimer))
                            {
                                sTimer += " ";
                            }
                            sTimer += "O:" + building.m_outgoingProblemTimer;
                        }
                    }
                    else
                    {
                        if (building.m_incomingProblemTimer > 0)
                        {
                            if (string.IsNullOrEmpty(sTimer))
                            {
                                sTimer += " ";
                            }
                            sTimer += "I:" + building.m_incomingProblemTimer;
                        }
                    }
                }
            } 

            return sTimer;
        }

        public override string GetTarget()
        {
            Building building = BuildingManager.instance.m_buildings.m_buffer[m_buildingId];
            ProcessingFacilityAI? buildingAI = building.Info?.m_buildingAI as ProcessingFacilityAI;
            if (buildingAI != null && m_material == buildingAI.m_outputResource)
            { 
                return ""; // A processing plant outgoing will never have a responder
            }
            else
            {
                return base.GetTarget();
            }
        }

        public override string GetResponder()
        {
            Building building = BuildingManager.instance.m_buildings.m_buffer[m_buildingId];
            ProcessingFacilityAI? buildingAI = building.Info?.m_buildingAI as ProcessingFacilityAI;
            if (buildingAI != null && m_material == buildingAI.m_outputResource)
            {
                return ""; // A processing plant outgoing will never have a responder
            }
            else
            {
                return base.GetResponder();
            }
        }
    }
}