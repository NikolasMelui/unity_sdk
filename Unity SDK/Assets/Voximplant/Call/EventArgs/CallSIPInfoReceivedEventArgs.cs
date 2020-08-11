/*
 *  Copyright (c) 2011-2020, Zingaya, Inc. All rights reserved.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Voximplant.Unity.@internal;

namespace Voximplant.Unity.Call.EventArgs
{
    /// <summary>
    /// Provide data for the <see cref="ICall.SIPInfoReceived"/> event.
    /// </summary>
    [Serializable]
    public class CallSIPInfoReceivedEventArgs : System.EventArgs
    {
        private IReadOnlyDictionary<string, string> _parsedHeaders;

        [SerializeField]
        private string content = default;

        [SerializeField]
        private IList<string> headers = default;

        [SerializeField]
        private string type = default;

        /// <summary>
        /// MIME type of INFO message.
        /// </summary>
        public string Type => type;

        /// <summary>
        /// Body of INFO message.
        /// </summary>
        public string Content => content;

        /// <summary>
        /// Optional SIP headers received with message.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers =>
            _parsedHeaders ?? (_parsedHeaders = JsonHelper.FromList(headers));
    }
}