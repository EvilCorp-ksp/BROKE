﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BROKE
{
    interface IFundingModifier
    {
        /// <summary>
        /// The name of the Funding Modifier (ie, Crew Payroll, Government Funding, etc)
        /// </summary>
        /// <returns>string Funding Modifier's Name</returns>
        string GetName();

        /// <summary>
        /// Gets the name to use for the ConfigNode (ie, BROKEPayroll, BROKEGovt, etc)
        /// </summary>
        /// <returns>string Funding Modifier's ConfigNode name</returns>
        string GetConfigName();

        /// <summary>
        /// Return the funds gained and lost for this Quarter. The returned values will be added/removed by B.R.O.K.E. so there is no need to do that yourself.
        /// </summary>
        /// <returns>double[2] income, expenses</returns>
        double[] ProcessQuarterly();

        /// <summary>
        /// Return the funds gained and lost Yearly. The returned values will be added/removed by B.R.O.K.E. so there is no need to do that yourself.
        /// Note that this should NOT be the sum of the Quarterly income/expenses for the year. That's calculated by B.R.O.K.E. This is just for
        /// income/expenses that happen ONLY once a year
        /// </summary>
        /// <returns>double[2] income, expenses</returns>
        double[] ProcessYearly();

        /// <summary>
        /// This will be called once a day and should be used to update any time-based funding information (ie, # of running missions that day, which Kerbals are at KSC and for how long, etc)
        /// </summary>
        void DailyUpdate();

        /// <summary>
        /// Saves any important data to a ConfigNode to be saved into Persistence, or null if nothing to save.
        /// Name of ConfigNode will be set by GetConfigName automatically
        /// </summary>
        /// <returns>ConfigNode containing data to save, or null</returns>
        ConfigNode SaveData();

        /// <summary>
        /// Loads the saved data into the FundingModifier.
        /// </summary>
        /// <param name="node">Source ConfigNode containing any pertinent information</param>
        void LoadData(ConfigNode node);

    }
}