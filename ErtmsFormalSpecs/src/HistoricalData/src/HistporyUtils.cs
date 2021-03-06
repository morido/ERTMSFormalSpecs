﻿// ------------------------------------------------------------------------------
// -- Copyright ERTMS Solutions
// -- Licensed under the EUPL V.1.1
// -- http://joinup.ec.europa.eu/software/page/eupl/licence-eupl
// --
// -- This file is part of ERTMSFormalSpec software and documentation
// --
// --  ERTMSFormalSpec is free software: you can redistribute it and/or modify
// --  it under the terms of the EUPL General Public License, v.1.1
// --
// -- ERTMSFormalSpec is distributed in the hope that it will be useful,
// -- but WITHOUT ANY WARRANTY; without even the implied warranty of
// -- MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// --
// ------------------------------------------------------------------------------
namespace HistoricalData
{
    using XmlBooster;
    using System.IO;

    /// <summary>
    /// Utility class for historical data
    /// </summary>
    public class HistoryUtils
    {
        /// <summary>
        /// The Logger
        /// </summary>
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Initializes the Historical data package
        /// </summary>
        /// <param name="filePath">The path to the file to load</param>
        /// <param name="factory">THe factory used to create instances of the loaded file</param>
        public static History Load(string filePath, Generated.Factory factory)
        {
            History retVal = null;

            Generated.acceptor.setFactory(factory);
            if (File.Exists(filePath))
            {
                // Do not rely on XmlBFileContext since it does not care about encoding. 
                // File encoding is UTF-8
                XmlBStringContext ctxt;
                using (StreamReader file = new StreamReader(filePath))
                {
                    ctxt = new XmlBStringContext(file.ReadToEnd());
                    file.Close();
                }

                try
                {
                    retVal = Generated.acceptor.accept(ctxt) as History;
                }
                catch (XmlBooster.XmlBException excp)
                {
                    Log.Error(ctxt.errorMessage());
                }
            }

            return retVal;
        }
    }
}
