using System.Collections.Generic;
using TransferManagerCE.Common;
using static TransferManager;
using static TransferManagerCE.BuildingTypeHelper;

namespace TransferManagerCE.TransferRules
{
    public class BuildingRuleSets
    {
        private static Dictionary<BuildingType, List<ReasonRule>> BuildingRules = new Dictionary<BuildingType, List<ReasonRule>>();
        private static readonly object s_dictionaryLock = new object();
        private static bool s_initNeeded = true;

        private static HashSet<TransferReason> s_districtReasons = new HashSet<TransferReason>();
        private static HashSet<TransferReason> s_buildingReasons = new HashSet<TransferReason>();
        private static HashSet<TransferReason> s_distanceReasons = new HashSet<TransferReason>();

        public static bool IsDistrictRestrictionsSupported(TransferReason material)
        {
            if (s_initNeeded)
            {
                lock (s_dictionaryLock)
                {
                    Init();
                }
            }

            return s_districtReasons.Contains(material);
        }

        public static bool IsBuildingRestrictionsSupported(TransferReason material)
        {
            if (s_initNeeded)
            {
                lock (s_dictionaryLock)
                {
                    Init();
                }
            }

            return s_buildingReasons.Contains(material);
        }

        public static bool IsDistanceRestrictionsSupported(TransferReason material)
        {
            if (s_initNeeded)
            {
                lock (s_dictionaryLock)
                {
                    Init();
                }
            }

            return s_distanceReasons.Contains(material);
        }

        public static int GetRestrictionId(BuildingType eBuildingType, TransferReason material)
        {
            lock (s_dictionaryLock)
            {
                Init();

                if (BuildingRules.ContainsKey(eBuildingType))
                {
                    foreach (ReasonRule rule in BuildingRules[eBuildingType])
                    {
                        if (rule.m_reasons.Contains(material))
                        {
                            return rule.m_id;
                        }
                    }
                }
                return -1;
            }
        }

        public static bool HasIncomingDistrictRules(BuildingType eBuildingType, TransferReason material)
        {
            lock (s_dictionaryLock)
            {
                Init();

                if (BuildingRules.ContainsKey(eBuildingType))
                {
                    foreach (ReasonRule rule in BuildingRules[eBuildingType])
                    {
                        if (rule.m_reasons.Contains(material))
                        {
                            return rule.m_incomingDistrict;
                        }
                    }
                }

                return false;
            }
        }

        public static bool HasOutgoingDistrictRules(BuildingType eBuildingType, TransferReason material)
        {
            lock (s_dictionaryLock)
            {
                Init();

                if (BuildingRules.ContainsKey(eBuildingType))
                {
                    foreach (ReasonRule rule in BuildingRules[eBuildingType])
                    {
                        if (rule.m_reasons.Contains(material))
                        {
                            return rule.m_outgoingDistrict;
                        }
                    }
                }

                return false;
            }
        }

        public static bool HasDistanceRules(BuildingType eBuildingType, TransferReason material)
        {
            lock (s_dictionaryLock)
            {
                Init();

                if (BuildingRules.ContainsKey(eBuildingType))
                {
                    foreach (ReasonRule rule in BuildingRules[eBuildingType])
                    {
                        if (rule.m_reasons.Contains(material))
                        {
                            return rule.m_distance;
                        }
                    }
                }

                return false;
            }
        }

        public static List<ReasonRule> GetRules(BuildingType eBuildingType)
        {
            lock (s_dictionaryLock)
            {
                Init();
                if (BuildingRules.ContainsKey(eBuildingType))
                {
                    return BuildingRules[eBuildingType];
                }
                else
                {
                    return new List<ReasonRule>();
                }
            }
        }
        
        public static List<ReasonRule> GetRules(BuildingType eBuildingType, ushort buildingId)
        {
            lock (s_dictionaryLock)
            {
                Init();

                if (BuildingRules.ContainsKey(eBuildingType))
                {
                    List<ReasonRule> buildingRules = new List<ReasonRule>(BuildingRules[eBuildingType]);

                    // Select appropriate rulesets for certain types
                    switch (eBuildingType)
                    {
                        case BuildingType.Warehouse:
                            {
                                // Warehouses, just return the actual material they store
                                List<ReasonRule> rules = new List<ReasonRule>();

                                CustomTransferReason actualTransferReason = GetWarehouseTransferReason(buildingId);
                                if (actualTransferReason != null)
                                {
                                    foreach (ReasonRule rule in buildingRules)
                                    {
                                        if (rule != null && rule.m_reasons.Contains(actualTransferReason))
                                        {
                                            rules.Add(rule);
                                            break;
                                        }
                                    }
                                }

                                return rules;
                            }
                        case BuildingType.CargoFerryWarehouseHarbor:
                            {
                                // Warehouses, just return the actual material they store
                                List<ReasonRule> rules = new List<ReasonRule>();

                                TransferReason material = GetCargoFerryWarehouseActualTransferReason(buildingId);
                                if (material != TransferReason.None)
                                {
                                    foreach (ReasonRule rule in buildingRules)
                                    {
                                        if (rule != null && rule.m_reasons.Contains(material))
                                        {
                                            rules.Add(rule);
                                            break;
                                        }
                                    }
                                }
                                
                                return rules;
                            }
                        case BuildingType.UniqueFactory:
                            {
                                // Remove outgoing rule if unique factory has no vehicles.
                                if (!HasVehicles(buildingId))
                                {
                                    List<ReasonRule> rules = new List<ReasonRule>
                                    {
                                        buildingRules[0]
                                    };
                                    return rules;
                                }
                                break;
                            }
                    }

                    return buildingRules;
                }
            }

            return new List<ReasonRule>();
        }

        private static void Init()
        {
            if (s_initNeeded)
            {
                s_initNeeded = false;
                BuildingRules.Clear();

                ElementarySchool();
                HighSchool();
                University();
                AirportMainTerminal();

                // Services
                Cemetery();
                Hospital();
                MedicalHelicopterDepot();
                PoliceStation();
                PoliceHelicopterDepot();
                Prison();
                Bank();
                FireStation();
                FireHelicopterDepot();
                ParkMaintenanceDepot();
                RoadMaintenanceDepot();
                TaxiDepot();
                TaxiStand();
                DisasterResponseUnit();
                SnowDump();

                // Garbage
                LandFill();
                IncinerationPlant();
                Recycling();
                WasteTransfer();
                WasteProcessing();

                // Mail
                PostOffice();
                PostSortingFacility();

                Commercial();
                MainIndustryBuilding();
                ExtractionFacility();
                ProcessingFacility();
                UniqueFactory();

                GenericExtractor();
                GenericProcessing();
                GenericFactory();

                FishFarm();
                FishHarbor();
                FishFactory();
                FishMarket();

                Warehouse();
                OutsideConnection();

                CoalPowerPlant();
                PetrolPowerPlant();
                BoilerPlant();
                DisasterShelter();
                PumpingService();

                // Load transfer reasons into HashSet so we can check if supported
                foreach (KeyValuePair<BuildingType, List<ReasonRule>> kvp in BuildingRules)
                {
                    foreach (ReasonRule rule in kvp.Value)
                    {
                        // Districts
                        if (rule.m_incomingDistrict || rule.m_outgoingDistrict)
                        {
                            foreach (TransferReason material in rule.m_reasons)
                            {
                                s_districtReasons.Add(material);
                            }
                        }

                        // Buildings
                        if (rule.m_incomingBuilding || rule.m_outgoingBuilding)
                        {
                            foreach (TransferReason material in rule.m_reasons)
                            {
                                s_buildingReasons.Add(material);
                            }
                        }

                        // Distance
                        if (rule.m_distance)
                        {
                            foreach (TransferReason material in rule.m_reasons)
                            {
                                s_distanceReasons.Add(material);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////
        /// Services
        /// </summary>
        
        private static void ElementarySchool()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonStudent1"); //"Students";
                rule.AddReason(TransferReason.Student1);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.ElementartySchool] = list;
        }
        private static void HighSchool()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonStudent2"); //"Students";
                rule.AddReason(TransferReason.Student2);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.HighSchool] = list;
        }
        private static void University()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonStudent3"); //"Students";
                rule.AddReason(TransferReason.Student3);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.University] = list;
        }

        private static void AirportMainTerminal()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonCrime"); //"Crime";
                rule.AddReason(TransferReason.Crime);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonGarbage"); //"Garbage";
                rule.AddReason(TransferReason.Garbage);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.AirportMainTerminal] = list;
            BuildingRules[BuildingType.AirportCargoTerminal] = list;
        }

        private static void Cemetery()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            // Dead
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonDead"); //"Collecting Dead";
                rule.AddReason(TransferReason.Dead);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            // DeadMove
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonDeadMove"); //"Moving Dead";
                rule.AddReason(TransferReason.DeadMove);
                rule.m_incomingDistrict = true;
                rule.m_outgoingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.Cemetery] = list;
        }

        private static void Hospital()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            // Sick
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonSick"); //"Collecting Sick";
                rule.AddReason(TransferReason.Sick);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            // SickMove, IN from medical helicopters
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonSickMove"); //"Moving Sick";
                rule.AddReason(TransferReason.SickMove);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.Hospital] = list;
        }
        private static void MedicalHelicopterDepot()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            // Sick2 IN to request a helicopter
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonSick"); //"Collecting Sick";
                rule.AddReason(TransferReason.Sick2);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            // SickMove OUT (From helicopter) after picking up a sick patient this is used to find a nearby hospital
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonSickMove"); //"Moving Sick";
                rule.AddReason(TransferReason.SickMove);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.MedicalHelicopterDepot] = list;
        }
        
        private static void PoliceStation()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            // Crime
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonCrime"); //"Crime";
                rule.AddReason(TransferReason.Crime);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            // CrimeMove
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonCrimeMove"); //"Moving Criminals";
                rule.AddReason(TransferReason.CriminalMove);
                rule.m_incomingDistrict = true;
                rule.m_outgoingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.PoliceStation] = list;
        }
        private static void PoliceHelicopterDepot()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            // Crime
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonCrime"); //"Crime";
                rule.AddReason((TransferReason) CustomTransferReason.Reason.Crime2);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            // CrimeMove
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonCrimeMove"); //"Moving Criminals";
                rule.AddReason(TransferReason.CriminalMove);
                rule.m_incomingDistrict = true;
                rule.m_outgoingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.PoliceHelicopterDepot] = list;
        }
        private static void Prison()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            // CrimeMove
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonCrimeMove"); //"Moving Criminals";
                rule.AddReason(TransferReason.CriminalMove);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.Prison] = list;
        }
        private static void Bank()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            // CrimeMove
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonCash"); //"Moving Criminals";
                rule.AddReason(TransferReason.Cash);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.Bank] = list;
        }
        
        private static void FireStation()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonFire"); //"Fire";
                rule.AddReason(TransferReason.Fire);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.FireStation] = list;
        }
        private static void FireHelicopterDepot()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonFire2"); //"Fire Helicopter";
                rule.AddReason(TransferReason.Fire2);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            { 
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonForestFire"); //"Forest Fire";
                rule.AddReason(TransferReason.ForestFire);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.FireHelicopterDepot] = list;
        }
        private static void LandFill()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonGarbage"); //"Garbage Collection";
                rule.AddReason(TransferReason.Garbage);
                rule.m_incomingDistrict = true; // Active
                rule.m_incomingBuilding = true; // Active
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonGarbageMove"); //"Garbage Move";
                rule.AddReason(TransferReason.GarbageMove);
                rule.m_incomingDistrict = true; // Passive
                rule.m_outgoingDistrict = true; // Active
                rule.m_incomingBuilding = true; // Passive
                rule.m_outgoingBuilding = true; // Active
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 2;
                rule.m_name = Localization.Get("reasonGarbageTransfer"); //"Garbage Transfer";
                rule.AddReason(TransferReason.GarbageTransfer);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true; // Active
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.Landfill] = list;
        }
        private static void IncinerationPlant()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonGarbage"); //"Garbage Collection";
                rule.AddReason(TransferReason.Garbage);
                rule.m_incomingDistrict = true; // Active
                rule.m_incomingBuilding = true; // Active
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonGarbageMove"); //"Garbage Move";
                rule.AddReason(TransferReason.GarbageMove);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true; // Active
                list.Add(rule);
            }

            BuildingRules[BuildingType.IncinerationPlant] = list;
        }
        private static void Recycling()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonGarbage"); //"Garbage Collection";
                rule.AddReason(TransferReason.Garbage);
                rule.m_incomingDistrict = true; // Active
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonGarbageMove"); //"Garbage Move";
                rule.AddReason(TransferReason.GarbageMove);
                rule.m_incomingDistrict = true; // Passive from land fills
                rule.m_incomingBuilding = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 2;
                rule.m_name = Localization.Get("reasonMaterialOut"); //"Outgoing Material";
                rule.AddReason(TransferReason.Coal);
                rule.AddReason(TransferReason.Lumber);
                rule.AddReason(TransferReason.Petrol);
                rule.m_outgoingDistrict = true; // Active
                rule.m_outgoingBuilding = true; // Active
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.Recycling] = list;
        }
        private static void WasteTransfer()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            { 
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonGarbage"); //"Garbage Collection";
                rule.AddReason(TransferReason.Garbage);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonGarbageMove"); //"Garbage Move";
                rule.AddReason(TransferReason.GarbageMove);
                rule.m_incomingDistrict = true; // Passive from land fills
                rule.m_outgoingDistrict = true; // When in "Empty" mode
                rule.m_incomingBuilding = true; // Passive from land fills
                rule.m_outgoingBuilding = true; // When in "Empty" mode
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 2;
                rule.m_name = Localization.Get("reasonGarbageTransfer"); //"Garbage Transfer";
                rule.AddReason(TransferReason.GarbageTransfer);
                rule.m_outgoingDistrict = true; // Passive
                rule.m_outgoingBuilding = true; // Passive
                list.Add(rule);
            }

            BuildingRules[BuildingType.WasteTransfer] = list;
        }
        private static void WasteProcessing()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonGarbageTransfer"); //"Garbage Transfer";
                rule.AddReason(TransferReason.GarbageTransfer);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonMaterialOut"); //"Outgoing Material";
                rule.AddReason(TransferReason.Coal);
                rule.AddReason(TransferReason.Lumber);
                rule.AddReason(TransferReason.Petrol);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.WasteProcessing] = list;
        }
        private static void PostOffice()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonMail"); //"Mail";
                rule.AddReason(TransferReason.Mail);
                rule.m_incomingDistrict = true; // Active
                rule.m_incomingBuilding = true; // Active
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonUnsortedMail"); //"Unsorted Mail";
                rule.AddReason(TransferReason.UnsortedMail);
                rule.m_outgoingDistrict = true; // Active
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 2;
                rule.m_name = Localization.Get("reasonSortedMail"); //"Sorted Mail";
                rule.AddReason(TransferReason.SortedMail);
                rule.m_incomingDistrict = true; // Passive
                rule.m_incomingBuilding = true; // Passive
                rule.m_import = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.PostOffice] = list;
        }
        private static void PostSortingFacility()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonUnsortedMail"); //"Unsorted Mail";
                rule.AddReason(TransferReason.UnsortedMail);
                rule.AddReason(TransferReason.OutgoingMail);
                rule.m_incomingDistrict = true; // Passive
                rule.m_incomingBuilding = true; // Passive
                rule.m_import = true;
                rule.m_export = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonSortedMail"); //"Sorted Mail";
                rule.AddReason(TransferReason.SortedMail);
                rule.AddReason(TransferReason.IncomingMail);
                rule.m_outgoingDistrict = true; // Active
                rule.m_outgoingBuilding = true; // Active
                rule.m_distance = true;
                rule.m_import = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.PostSortingFacility] = list;
        }
        private static void ParkMaintenanceDepot()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonParkMaintenance"); //"Park Maintenance";
                rule.AddReason(TransferReason.ParkMaintenance);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            BuildingRules[BuildingType.ParkMaintenanceDepot] = list;
        }
        private static void RoadMaintenanceDepot()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonRoadMaintenance"); //"Road Maintenance";
                rule.AddReason(TransferReason.RoadMaintenance);
                rule.m_outgoingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            BuildingRules[BuildingType.RoadMaintenanceDepot] = list;
        }
        private static void TaxiDepot()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonTaxi"); //"Taxi";
                rule.AddReason(TransferReason.Taxi);
                rule.m_outgoingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            BuildingRules[BuildingType.TaxiDepot] = list;
        }
        private static void TaxiStand()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonTaxi"); //"Taxi";
                rule.AddReason(TransferReason.Taxi);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            BuildingRules[BuildingType.TaxiStand] = list;
        }
        private static void DisasterResponseUnit()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // Raw products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonCollapsed"); //"Trucks";
                rule.AddReason(TransferReason.Collapsed);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                // Outgoing product
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonCollapsed2"); //"Helicopters";
                rule.AddReason(TransferReason.Collapsed2);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.DisasterResponseUnit] = list;
        }
        private static void SnowDump()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // Raw products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonSnow");
                rule.AddReason(TransferReason.Snow);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }
            {
                // Outgoing product
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonSnowMove");
                rule.AddReason(TransferReason.SnowMove);
                rule.m_incomingDistrict = true;
                rule.m_outgoingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.SnowDump] = list;
        }
        

        private static void CoalPowerPlant()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Coal);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_import = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.CoalPowerPlant] = list;
        }
        private static void PetrolPowerPlant()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Petrol);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_import = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.PetrolPowerPlant] = list;
        }
        private static void BoilerPlant()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Petrol);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_import = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.BoilerStation] = list;
        }
        private static void DisasterShelter()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Goods);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_import = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.DisasterShelter] = list;
        }

        private static void PumpingService()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonFloodWater");
                rule.AddReason(TransferReason.FloodWater);
                rule.m_incomingDistrict = true;
                rule.m_distance = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.PumpingService] = list;
        }

        /// <summary>
        /// ////////////////////////////////////////////////////////////////
        /// Goods
        /// </summary>
        private static void Commercial()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial") + " 1";// "Incoming Goods";
                rule.AddReason(TransferReason.Goods);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_import = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonIncomingMaterial") + " 2";// "Incoming LuxuryProducts";
                rule.AddReason(TransferReason.LuxuryProducts);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.Commercial] = list;
        }
        private static void MainIndustryBuilding()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonCrime"); //"Crime";
                rule.AddReason(TransferReason.Crime);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonGarbage"); //"Garbage";
                rule.AddReason(TransferReason.Garbage);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.MainIndustryBuilding] = list;
        }
        private static void ExtractionFacility()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonRawMaterial"); //"Raw Material";
                rule.AddReason(TransferReason.Oil);
                rule.AddReason(TransferReason.Grain);
                rule.AddReason(TransferReason.Ore);
                rule.AddReason(TransferReason.Logs);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.ExtractionFacility] = list;
        }
        private static void ProcessingFacility()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // Raw products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Oil);
                rule.AddReason(TransferReason.Grain);
                rule.AddReason(TransferReason.Ore);
                rule.AddReason(TransferReason.Logs);
                rule.AddReason(TransferReason.Coal);
                rule.AddReason(TransferReason.Lumber);
                rule.AddReason(TransferReason.Petrol);
                rule.AddReason(TransferReason.Food);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_import = true;
                list.Add(rule);
            }
            {
                // DLC intermediate products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonOutgoingMaterial"); //"Outgoing Material";
                rule.AddReason(TransferReason.PlanedTimber);
                rule.AddReason(TransferReason.Paper);
                rule.AddReason(TransferReason.Glass);
                rule.AddReason(TransferReason.Metals);
                rule.AddReason(TransferReason.Petroleum);
                rule.AddReason(TransferReason.Plastics);
                rule.AddReason(TransferReason.AnimalProducts);
                rule.AddReason(TransferReason.Flours);
                rule.AddReason(TransferReason.Goods);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.ProcessingFacility] = list;
        }
        private static void UniqueFactory()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // Raw products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Grain);
                rule.AddReason(TransferReason.PlanedTimber);
                rule.AddReason(TransferReason.Paper);
                rule.AddReason(TransferReason.Glass);
                rule.AddReason(TransferReason.Metals);
                rule.AddReason(TransferReason.Petroleum);
                rule.AddReason(TransferReason.Plastics);
                rule.AddReason(TransferReason.AnimalProducts);
                rule.AddReason(TransferReason.Flours);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                list.Add(rule);
            }
            {
                // Outgoing product
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonOutgoingMaterial"); //"Outgoing Material";
                rule.AddReason(TransferReason.LuxuryProducts);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.UniqueFactory] = list;
        }
        private static void GenericExtractor()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // Raw products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonOutgoingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Oil);
                rule.AddReason(TransferReason.Grain);
                rule.AddReason(TransferReason.Ore);
                rule.AddReason(TransferReason.Logs);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.GenericExtractor] = list;
        }
        private static void GenericProcessing()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // Raw products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Oil);
                rule.AddReason(TransferReason.Grain);
                rule.AddReason(TransferReason.Ore);
                rule.AddReason(TransferReason.Logs);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                rule.m_import = true;
                list.Add(rule);
            }
            {
                // Generic intermediate products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonOutgoingMaterial"); //"Outgoing Material";
                rule.AddReason(TransferReason.Coal);
                rule.AddReason(TransferReason.Lumber);
                rule.AddReason(TransferReason.Petrol);
                rule.AddReason(TransferReason.Food);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.GenericProcessing] = list;
        }
        private static void GenericFactory()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // Raw products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial") + " 1";

                // Generic production materials
                rule.AddReason(TransferReason.Lumber);
                rule.AddReason(TransferReason.Petrol);
                rule.AddReason(TransferReason.Food);
                rule.AddReason(TransferReason.Coal);

                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = false;
                rule.m_import = true;
                list.Add(rule);
            }
            {
                // Raw products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 2;
                rule.m_name = Localization.Get("reasonIncomingMaterial") + " 2";

                // DLC production materials
                rule.AddReason(TransferReason.PlanedTimber);
                rule.AddReason(TransferReason.Paper);
                rule.AddReason(TransferReason.Glass);
                rule.AddReason(TransferReason.Metals);
                rule.AddReason(TransferReason.Petroleum);
                rule.AddReason(TransferReason.Plastics);
                rule.AddReason(TransferReason.AnimalProducts);
                rule.AddReason(TransferReason.Flours);

                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = false;
                list.Add(rule);
            }
            {
                // Generic factory output
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonOutgoingMaterial");
                rule.AddReason(TransferReason.Goods);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.GenericFactory] = list;
        }
        private static void FishFarm()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // DLC intermediate products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonOutgoingMaterial"); //"Outgoing Material";
                rule.AddReason(TransferReason.Fish);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.FishFarm] = list;
        }
        private static void FishHarbor()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // DLC intermediate products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonOutgoingMaterial"); //"Outgoing Material";
                rule.AddReason(TransferReason.Fish);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.FishHarbor] = list;
        }
        private static void FishFactory()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Fish);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                rule.m_import = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonOutgoingMaterial"); //"Outgoing Material";
                rule.AddReason(TransferReason.Goods);
                rule.m_outgoingDistrict = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.FishFactory] = list;
        }
        private static void FishMarket()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonIncomingMaterial"); //"Incoming Material";
                rule.AddReason(TransferReason.Fish);
                rule.m_incomingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_distance = true;
                rule.m_import = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.FishMarket] = list;
        }
        private static void Warehouse()
        {
            List<ReasonRule> list = new List<ReasonRule>();

            {
                // DLC intermediate products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonWarehouse"); //"Storage";
                rule.AddReason(TransferReason.PlanedTimber);
                rule.AddReason(TransferReason.Paper);
                rule.AddReason(TransferReason.Glass);
                rule.AddReason(TransferReason.Metals);
                rule.AddReason(TransferReason.Petroleum);
                rule.AddReason(TransferReason.Plastics);
                rule.AddReason(TransferReason.AnimalProducts);
                rule.AddReason(TransferReason.Flours);
                rule.AddReason(TransferReason.LuxuryProducts);
                rule.AddReason(TransferReason.Fish);
                rule.m_incomingDistrict = true;
                rule.m_outgoingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_export = true;
                list.Add(rule);
            }
            {
                // Generic industries intermediate products
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonWarehouse"); //"Storage";
                rule.AddReason(TransferReason.Oil);
                rule.AddReason(TransferReason.Ore);
                rule.AddReason(TransferReason.Logs);
                rule.AddReason(TransferReason.Grain);
                rule.AddReason(TransferReason.Coal);
                rule.AddReason(TransferReason.Lumber);
                rule.AddReason(TransferReason.Petrol);
                rule.AddReason(TransferReason.Food);
                rule.AddReason(TransferReason.Goods);
                rule.m_incomingDistrict = true;
                rule.m_outgoingDistrict = true;
                rule.m_incomingBuilding = true;
                rule.m_outgoingBuilding = true;
                rule.m_distance = true;
                rule.m_import = true;
                rule.m_export = true;
                list.Add(rule);
            }

            BuildingRules[BuildingType.Warehouse] = list;
            BuildingRules[BuildingType.CargoFerryWarehouseHarbor] = list;
        }
        private static void OutsideConnection()
        {
            List<ReasonRule> list = new List<ReasonRule>();
            
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 0;
                rule.m_name = Localization.Get("reasonGoods"); //"Goods";
                rule.AddReason(TransferReason.Oil);
                rule.AddReason(TransferReason.Ore);
                rule.AddReason(TransferReason.Logs);
                rule.AddReason(TransferReason.Grain);
                rule.AddReason(TransferReason.Goods);
                rule.AddReason(TransferReason.Oil);
                rule.AddReason(TransferReason.Ore);
                rule.AddReason(TransferReason.Logs);
                rule.AddReason(TransferReason.Grain);
                rule.AddReason(TransferReason.Goods);
                rule.AddReason(TransferReason.Coal);
                rule.AddReason(TransferReason.Lumber);
                rule.AddReason(TransferReason.Petrol);
                rule.AddReason(TransferReason.Food);
                rule.AddReason(TransferReason.PlanedTimber);
                rule.AddReason(TransferReason.Paper);
                rule.AddReason(TransferReason.Glass);
                rule.AddReason(TransferReason.Metals);
                rule.AddReason(TransferReason.Petroleum);
                rule.AddReason(TransferReason.Plastics);
                rule.AddReason(TransferReason.AnimalProducts);
                rule.AddReason(TransferReason.Flours);
                rule.AddReason(TransferReason.LuxuryProducts);
                rule.AddReason(TransferReason.Fish);
                rule.m_import = true;
                rule.m_export = true;
                list.Add(rule);
            }
            {
                ReasonRule rule = new ReasonRule();
                rule.m_id = 1;
                rule.m_name = Localization.Get("reasonMail"); //"Mail";
                rule.AddReason(TransferReason.SortedMail);
                rule.AddReason(TransferReason.IncomingMail);
                rule.AddReason(TransferReason.UnsortedMail);
                rule.AddReason(TransferReason.OutgoingMail);
                rule.m_import = true;
                rule.m_export = true;
                list.Add(rule);
            }
            BuildingRules[BuildingType.OutsideConnection] = list;
        }
    }
}
