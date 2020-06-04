using Autodesk.Revit.DB;
//
// (C) Copyright 2003-2014 by Autodesk, Inc.
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE. AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
//
// Use, duplication, or disclosure by the U.S. Government is subject to
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.
//
using System;
using System.Collections.Generic;
using System.Threading;

namespace FamilyEditorInterface
{
    /// <summary>
    ///   A list of requests the dialog has available
    /// </summary>
    /// 
    public enum RequestId : int
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// "Change parameter value" request
        /// </summary>
        ChangeParam = 1,
        /// <summary>
        /// "Delete an element" request
        /// </summary>
        DeleteId = 2,
        /// <summary>
        /// "Regenerate Document" request
        /// </summary>
        RestoreAll = 3,
        /// <summary>
        /// "Change Parameter Name" request
        /// </summary>
        ChangeParamName = 4,
        /// <summary>
        /// Converts Type to Instance
        /// </summary>
        TypeToInstance = 5,
        /// <summary>
        /// "Shuffle parameters" request
        /// </summary>
        ShuffleParam = 6,
    }
    /// <summary>
    ///   A class around a variable holding the current request.
    /// </summary>
    /// <remarks>
    ///   Access to it is made thread-safe, even though we don't necessarily
    ///   need it if we always disable the dialog between individual requests.
    /// </remarks>
    /// 
    public class Request
    {
        // Storing the value as a plain Int makes using the interlocking mechanism simpler
        private int m_request = (int)RequestId.None;
        // try tp transport information
        private Tuple<string, double> value;
        private List<Tuple<string, double>> shuffleValue;
        private List<Tuple<string, string>> renameValue;
        private List<string> deleteValue;
        private List<string> typeToInstance;
        private List<Tuple<string, string, double>> allValues;
        /// <summary>
        ///   Take - The Idling handler calls this to obtain the latest request. 
        /// </summary>
        /// <remarks>
        ///   This is not a getter! It takes the request and replaces it
        ///   with 'None' to indicate that the request has been "passed on".
        /// </remarks>
        /// 
        public RequestId Take()
        {
            return (RequestId)Interlocked.Exchange(ref m_request, (int)RequestId.None);
        }
        /// <summary>
        ///   Make - The Dialog calls this when the user presses a command button there. 
        /// </summary>
        /// <remarks>
        ///   It replaces any older request previously made.
        /// </remarks>
        /// 
        public void Make(RequestId request)
        {
            Interlocked.Exchange(ref m_request, (int)request);
        }

        #region Values
        //Contains a single value to change
        internal void Value(Tuple<string, double> value)
        {
            this.value = value;
        }
        //Contains the shuffle values, a list of tupples
        internal void ShuffleValue(List<Tuple<string, double>> svalue)
        {
            this.shuffleValue = svalue;
        }
        //Contains all the values that needs to be renamed - when would that ever be the case? Why would we need a list?
        internal void RenameValue(List<Tuple<string, string>> renameValue)
        {
            this.renameValue = renameValue;
        }
        //Contains a list of all the parameters that will be deleted
        internal void DeleteValue(List<string> deleteValue)
        {
            this.deleteValue = deleteValue;
        }
        internal void AllValues(List<Tuple<string, string, double>> allValues)
        {
            this.allValues = allValues;
        }
        //Contains the parameters that will be toggled form instance to type and vica verse
        internal void TypeToInstance(List<string> typeToInstance)
        {
            this.typeToInstance = typeToInstance;
        }
        #endregion

        #region GetValue
        // try to transport the message
        internal List<string> GetDeleteValue()
        {
            return this.deleteValue;
        }
        // try to transport the message
        internal Tuple<string, double> GetValue()
        {
            return this.value;
        }        
        // try to transport the message
        internal List<Tuple<string, double>> GetShuffleValue()
        {
            return this.shuffleValue;
        }
        // try to transport the message
        internal List<Tuple<string, string>> GetRenameValue()
        {
            return this.renameValue;
        }
        // try to transport the message
        internal List<string> GetTypeToInstanceValue()
        {
            return this.typeToInstance;
        }
        // try to transport the message
        internal List<Tuple<string, string, double>> GetAllValues()
        {
            return this.allValues;
        }
        #endregion
    }
}
