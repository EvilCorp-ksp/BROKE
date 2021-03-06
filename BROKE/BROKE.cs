﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPPluginFramework;
using System.Reflection;

namespace BROKE
{
    
    //Utilizes the amazing KSP Plugin Framework by TriggerAU to make things much easier
    //http://forum.kerbalspaceprogram.com/threads/66503-KSP-Plugin-Framework-Plugin-Examples-and-Structure-v1-1-(Apr-6)

    [KSPAddon((KSPAddon.Startup.SpaceCentre), false)]
    public class BROKE_SpaceCenter : BROKE { }
    [KSPAddon((KSPAddon.Startup.TrackingStation), false)]
    public class BROKE_TrackingStation : BROKE { }
    [KSPAddon((KSPAddon.Startup.EditorAny), false)]
    public class BROKE_Editor : BROKE { }
    [KSPAddon((KSPAddon.Startup.Flight), false)]
    public class BROKE_Flight : BROKE { }

    public class BROKE : MonoBehaviourWindow
    {
        public static BROKE Instance;

        public int sPerDay = KSPUtil.Day, sPerYear = KSPUtil.Year, sPerQuarter = KSPUtil.Year / 4;
        private int LastUT = -1;

        public List<IFundingModifier> fundingModifiers;
        internal readonly List<InvoiceItem> InvoiceItems = new List<InvoiceItem>();
        public List<string> disabledFundingModifiers = new List<string>();

        public double RemainingDebt()
        {
            return InvoiceItems.Sum(item => item.Expenses);
        }

        public double PendingRevenue()
        {
            return InvoiceItems.Sum(item => item.Revenue);
        }

        private int WindowWidth = 360, WindowHeight = 540;
        private bool DrawSettings = false;

        private ApplicationLauncherButton button;

        private List<string> skins = new List<string> { "KSP", "Unity", "Mixed" };
        public int SelectedSkin = 0;

        internal override void Start()
        {
            if (Instance != null)
            {
                Destroy(Instance);
            }
            Instance = this;
            LogFormatted_DebugOnly("Printing names of all classes implementing IFundingModifier:");
            fundingModifiers = GetFundingModifiers();
            foreach (IFundingModifier fundMod in fundingModifiers)
            {
                LogFormatted_DebugOnly(fundMod.GetName());
            }

            if (ApplicationLauncher.Instance != null && ApplicationLauncher.Ready)
                OnAppLauncherReady();
            else
                GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
        }

        internal override void OnDestroy()
        {
           // GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
            ApplicationLauncher.Instance.RemoveModApplication(button);
        }


        internal override void FixedUpdate()
        {
            //Check the time and see if a new day, quarter, year has started and if so trigger the appropriate calls
            //We'll do this with the modulus function, rather than tracking the amount of time that has passed since the last update
            bool doExpenseReport = false;
            int UT = (int)Planetarium.GetUniversalTime();
            if (LastUT < 0)
                LastUT = UT;
            if (UT % sPerDay < LastUT % sPerDay) //if UT and LastUT are same day, then UT % sPerDay is > LastUT % sPerDay. If they're different days, then UT % sPerDay will be < LastUT % sPerDay
            {
                //new day
                NewDay();
            }
            if (UT % sPerQuarter < LastUT % sPerQuarter)
            {
                //New quarter
                NewQuarter();
                doExpenseReport = true;
            }
            if (UT % sPerYear < LastUT % sPerYear)
            {
                //New year
                NewYear();
            }

            LastUT = UT;

            if (doExpenseReport)
            {
                ProcessExpenseReport();
            }
        }

        public void ProcessExpenseReport()
        {
           // DisplayExpenseReport();
            button.SetTrue();
        }

        internal override void DrawWindow(int id)
        {
            if (DrawSettings)
                DrawSettingsWindow();
            else
                DrawExpenseReportWindow();
        }


        void OnAppLauncherReady()
        {
            if (button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);
                button = null;
            }
            this.button = ApplicationLauncher.Instance.AddModApplication(
                DisplayExpenseReport,
                () => { this.Visible = false; },
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.TRACKSTATION,
                (Texture)GameDatabase.Instance.GetTexture("BROKE/Textures/icon_button_stock", false));
        }


        private string payAmountTxt = "-1";
        private IFundingModifier selectedMainFM;
        private Vector2 revenueScroll, expenseScroll, customScroll;
        public void DrawExpenseReportWindow()
        {
            GUIStyle redText = new GUIStyle(GUI.skin.label);
            redText.normal.textColor = Color.red;
            GUIStyle yellowText = new GUIStyle(GUI.skin.label);
            yellowText.normal.textColor = Color.yellow;
            GUIStyle greenText = new GUIStyle(GUI.skin.label);
            greenText.normal.textColor = Color.green;

            GUIStyle redText2 = new GUIStyle(GUI.skin.textArea);
            redText2.normal.textColor = Color.red;
            GUIStyle yellowText2 = new GUIStyle(GUI.skin.textArea);
            yellowText2.normal.textColor = Color.yellow;
            GUIStyle greenText2 = new GUIStyle(GUI.skin.textArea);
            greenText2.normal.textColor = Color.green;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(WindowWidth));
            GUILayout.Label("Revenue", yellowText);
            //Wrap this in a scrollbar
            revenueScroll = GUILayout.BeginScrollView(revenueScroll, SkinsLibrary.CurrentSkin.textArea);
            double totalRevenue = 0;
            foreach (IFundingModifier FM in fundingModifiers)
            {
                var revenueForFM = InvoiceItems.Where(item => item.InvoiceName == FM.GetName()).Sum(item => item.Revenue);
                totalRevenue += revenueForFM;
                if (revenueForFM != 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(FM.GetName(), SkinsLibrary.CurrentSkin.textArea, GUILayout.Width(WindowWidth/2));
                    GUILayout.Label("√" + revenueForFM.ToString("N"), greenText2); //GREEN
                    if (FM.hasMainGUI())
                    {
                        if (selectedMainFM != FM && GUILayout.Button("→", GUILayout.ExpandWidth(false)))
                        {
                            //tell it to display Main stuff for this in a new vertical
                            selectedMainFM = FM;
                            this.WindowRect.width *= 2;
                            //widen the window probably
                        }
                        if (selectedMainFM != null && selectedMainFM == FM && GUILayout.Button("←", GUILayout.ExpandWidth(false)))
                        {
                            //hide side window
                            selectedMainFM = null;
                            this.WindowRect.width /= 2;
                            //maybe reset the width here then
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Revenue: ");
            GUILayout.Label("√" + totalRevenue.ToString("N"), greenText);
            GUILayout.EndHorizontal();

            GUILayout.Label("Expenses", yellowText);
            //Wrap this in a scrollbar
            expenseScroll = GUILayout.BeginScrollView(expenseScroll, SkinsLibrary.CurrentSkin.textArea);
            double totalExpenses = 0;
            foreach (IFundingModifier FM in fundingModifiers)
            {
                var expenseForFM = InvoiceItems.Where(item => item.InvoiceName == FM.GetName()).Sum(item => item.Expenses);
                totalExpenses += expenseForFM;
                if (expenseForFM != 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(FM.GetName(), SkinsLibrary.CurrentSkin.textArea, GUILayout.Width(WindowWidth / 2));
                    GUILayout.Label("√" + expenseForFM.ToString("N"), redText2); //RED
                    if (FM.hasMainGUI())
                    {
                        if (selectedMainFM != FM && GUILayout.Button("→", GUILayout.ExpandWidth(false)))
                        {
                            //tell it to display Main stuff for this in a new vertical
                            selectedMainFM = FM;
                            this.WindowRect.width *= 2;
                            //widen the window probably
                        }
                        if (selectedMainFM != null && selectedMainFM == FM && GUILayout.Button("←", GUILayout.ExpandWidth(false)))
                        {
                            //hide side window
                            selectedMainFM = null;
                            this.WindowRect.width /= 2;
                            //maybe reset the width here then
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Expenses: ");
            GUILayout.Label("√" + totalExpenses.ToString("N"), redText);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Net Revenue: ", yellowText);
            GUILayout.Label("√" + Math.Abs(totalRevenue - totalExpenses).ToString("N"), (totalRevenue - totalExpenses) < 0 ? redText : greenText);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(((Texture)GameDatabase.Instance.GetTexture("BROKE/Textures/gear", false)), GUILayout.ExpandWidth(false)))
            {
                DrawSettings = true;
            }
            payAmountTxt = GUILayout.TextField(payAmountTxt);
            if (GUILayout.Button("Pay", GUILayout.ExpandWidth(false)))
            {
                double toPay;
                if (!double.TryParse(payAmountTxt, out toPay))
                {
                    toPay = -1;
                    payAmountTxt = "Pay Maximum";
                }
                else
                {
                    toPay += PendingRevenue();
                }
                CashInRevenues();
                toPay = Math.Min(toPay, RemainingDebt());
                PayExpenses(toPay);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            //draw main window for the selected FM
            if (selectedMainFM != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(WindowWidth));
                customScroll = GUILayout.BeginScrollView(customScroll);
                //Put it in a scrollview as well
                selectedMainFM.DrawMainGUI();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        public void DrawSettingsWindow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            foreach (IFundingModifier FM in fundingModifiers)
            {
                GUILayout.BeginHorizontal(SkinsLibrary.CurrentSkin.textArea);
                GUILayout.Label(FM.GetName(), GUILayout.Width(WindowWidth / 2));
                bool disabled = FMDisabled(FM);
                if (GUILayout.Button(disabled ? "Enable" : "Disable"))
                {
                    if (disabled)
                        EnableFundingModifier(FM);
                    else
                        DisableFundingModifier(FM);
                }
                if (FM.hasSettingsGUI())
                {
                    if (selectedMainFM != FM && GUILayout.Button("→", GUILayout.ExpandWidth(false)))
                    {
                        //tell it to display Main stuff for this in a new vertical
                        selectedMainFM = FM;
                        this.WindowRect.width *= 2;
                        //widen the window probably
                    }
                    if (selectedMainFM != null && selectedMainFM == FM && GUILayout.Button("←", GUILayout.ExpandWidth(false)))
                    {
                        //hide side window
                        selectedMainFM = null;
                        this.WindowRect.width /= 2;
                        //maybe reset the width here then
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Change Skin"))
            {
                //SkinsLibrary.SetCurrent(SkinsLibrary.List.Keys.ElementAt((SkinsLibrary.List.Values.ToList().IndexOf(SkinsLibrary.CurrentSkin) + 1)%SkinsLibrary.List.Count));
                SelectedSkin++;
                SelectedSkin %= skins.Count;
                SelectSkin(SelectedSkin);
            }

            GUILayout.EndVertical();


            //draw main window for the selected FM
            if (selectedMainFM != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(WindowWidth));
                customScroll = GUILayout.BeginScrollView(customScroll);
                //Put it in a scrollview as well
                selectedMainFM.DrawSettingsGUI();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        public void SelectSkin(int skinID)
        {
            if (skinID == 0)
                SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.KSP);
            else if (skinID == 1)
                SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.Unity);
            else if (skinID == 2)
            {
                if (!SkinsLibrary.SkinExists("Mixed"))
                {
                    GUISkin mixedSkin = SkinsLibrary.CopySkin(SkinsLibrary.DefSkinType.Unity);
                    mixedSkin.window = HighLogic.Skin.window;
                    SkinsLibrary.AddSkin("Mixed", mixedSkin, false);
                }
                SkinsLibrary.SetCurrent("Mixed");
            }

            SelectedSkin = skinID;
        }

        public bool FMDisabled(IFundingModifier toCheck)
        {
            return disabledFundingModifiers.Contains(toCheck.GetConfigName());
        }

        public bool FMDisabled(string toCheck)
        {
            return disabledFundingModifiers.Contains(toCheck);
        }

        public void DisableFundingModifier(IFundingModifier toDisable)
        {
            disabledFundingModifiers.AddUnique(toDisable.GetConfigName());
            toDisable.OnDisabled();
        }

        public void EnableFundingModifier(IFundingModifier toEnable)
        {
            disabledFundingModifiers.Remove(toEnable.GetConfigName());
            toEnable.OnEnabled();
        }

        public void NewDay()
        {
            //Update every FundingModifier
            LogFormatted_DebugOnly("New Day! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach(IFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    fundingMod.DailyUpdate();
                }
            }
        }

        public void NewQuarter()
        {
            //Calculate quarterly expenses and display expense report (unless also a new year)
            LogFormatted_DebugOnly("New Quarter! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach (IFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    InvoiceItems.Add(fundingMod.ProcessQuarterly());
                }
            }
        }

        public void NewYear()
        {
            //Calculate yearly expenses and display expense report
            LogFormatted_DebugOnly("New Year! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach (IFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    InvoiceItems.Add(fundingMod.ProcessYearly());
                }
            }
        }

        private void DisplayExpenseReport()
        {
            DrawSettings = false;
            Debug.Log("Revenue:");
            foreach (var invoiceItem in InvoiceItems)
                Debug.Log(invoiceItem.InvoiceName + ": " + invoiceItem.Revenue);

            Debug.Log("Expenses:");
            foreach (var invoiceItem in InvoiceItems)
                Debug.Log(invoiceItem.InvoiceName + ": " + invoiceItem.Expenses);

            this.Visible = true;
            this.DragEnabled = true;
            this.WindowRect.Set((Screen.width - WindowWidth) / 2, (Screen.height - WindowHeight) / 2, WindowWidth, WindowHeight);
            this.WindowCaption = "B.R.O.K.E.";
            //SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.Unity);
        }

        public void CashInRevenues()
        {
            foreach (var invoiceItem in InvoiceItems)
            {
                AdjustFunds(invoiceItem.Revenue);
                invoiceItem.WithdrawRevenue();
            }
        }

        public double PayExpenses(double MaxToPay = -1)
        {
            LogFormatted_DebugOnly("Paying Expenses!");
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) //No funds outside of career
                return 0;

            double toPay = 0;
            if (MaxToPay < 0) //Pay anything we can
            {
                //pay the minimum of the remaining funds and the amount we owe
                toPay = Math.Min(RemainingDebt(), Funding.Instance.Funds);
            }
            else //Pay the minimum of the amount we have, the amount we're willing to pay, and the amount we owe
            {
                toPay = Math.Min(MaxToPay, Funding.Instance.Funds);
                toPay = Math.Min(toPay, RemainingDebt());
            }
            AdjustFunds(-toPay);
            for(int i = 0; i < InvoiceItems.Count; ++i)
            {
                var item = InvoiceItems[i];
                if(item.Expenses == 0)
                {
                    InvoiceItems.RemoveAt(i);
                    --i;
                }
                else if (toPay > 0)
                {
                    var amountToPay = Math.Min(item.Expenses, toPay);
                    item.PayInvoice(amountToPay);
                    if (item.Expenses == 0)
                    {
                        //Remove item and backtrack on list to get the next item correctly
                        InvoiceItems.RemoveAt(i);
                        --i;
                    }
                    toPay -= amountToPay;
                }
                else
                {
                    item.NotifyMissedPayment();
                }
            }
            return RemainingDebt();
        }

        //Shamelessly modified from this StackOverflow question/answer: http://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
        //http://stackoverflow.com/questions/26733/getting-all-types-that-implement-an-interface
        public List<IFundingModifier> GetFundingModifiers()
        {
            Type type = typeof(IFundingModifier);
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
            List<IFundingModifier> fundingMods = new List<IFundingModifier>();

            foreach (Type t in types)
            {
                fundingMods.Add(Activator.CreateInstance(t) as IFundingModifier);
            }

            return fundingMods;
        }

        public static void AddOrCreateInDictionary(Dictionary<string, double> dict, string key, double value)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, value);
            else
                dict[key] += value;
        }

        public static double AdjustFunds(double fundsToAdd, TransactionReasons reason = TransactionReasons.None)
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Funding.Instance.AddFunds(fundsToAdd, reason);
                return Funding.Instance.Funds;
            }
            return 0;
        }
    }
}
