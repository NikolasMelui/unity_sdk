﻿using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Invoice
{
    class InvSDK: MonoBehaviour, IUnitySDKCallbacks
    {
        private AndroidJavaClass jc = null;
        private AndroidJavaObject jo;
        public Action<String> LogMethod;

        public delegate void deligateOnLoginSuccessful(string displayName);
        public delegate void deligateOnLoginFailed(LoginFailureReason reason);
        public delegate void deligateOnOneTimeKeyGenerated(string key);
        public delegate void deligateOnConnectionSuccessful();
        public delegate void deligateOnConnectionClosed();
        public delegate void deligateOnConnectionFailedWithError(string reason);
        public delegate void deligateOnCallConnected(string callId, Dictionary<string, string> headers);
        public delegate void deligateOnCallDisconnected(string callId, Dictionary<string, string> headers);
        public delegate void deligateOnCallRinging(string callId, Dictionary<string, string> headers);
        public delegate void deligateOnCallFailed(string callId, int code, string reason, Dictionary<string, string> headers);
        public delegate void deligateOnCallAudioStarted(string callId);
        public delegate void deligatonIncomingCall(String callId, String from, String displayName, Boolean videoCall, Dictionary<String, String> headers);
        public delegate void deligateOnSIPInfoReceivedInCall(string callId, string type, string content, Dictionary<string, string> headers);
		public delegate void deligateOnMessageReceivedInCall(string callId, string text, Dictionary<string, string> headers);
        public delegate void deligateOnNetStatsReceived(string callId, int packetLoss);
		public delegate void deligateOnStartCall(string callId);

		/**
		Called when packet loss data is received from the Voximplant cloud
		@event OnNetStatsReceived
		@param {string} callid Call identifier
		@param {int} loss Packet loss from 0 (no loss) to 100 (all lost)
		*/
        public event deligateOnNetStatsReceived onNetStatsReceived;
		/**
		Called if Voximplant-specified 'message' is recived. It's a specialized type of a more general SIP 'info' message
		@event OnMessageReceivedInCall
		@param {string} callid Call identifier
		@param {string} text Message text
		@param {Dictionary} (Only for iOS) headers Optional SIP headers set by a info sender 
		*/
        public event deligateOnMessageReceivedInCall onMessageReceivedInCall;
		/**
		Called if SIP 'info' message is received. That message can be sent from a Voximplant cloud scenario or forwarded from a caller
		@event OnSIPInfoReceivedInCall
		@param {string} callid Call identifier
		@param {string} type Data MIME type string
		@param {string} content Data string
		@param {Dictionary} headers Optional SIP headers set by a info sender
		*/
        public event deligateOnSIPInfoReceivedInCall onSIPInfoReceivedInCall;
		/**
		Called when Voximplant directs a new call to a user logged in from this SDK instance. SDK can handle multiple incoming and/or outgoing calls at once and target specified call via the 'callid' string returned by 'call()' method and received by this event
		@event OnIncomingCall
		@param {string} callid Call identifier
		@param {string} from Caller SIP URI
		@param {string} name Caller display name
		@param {bool} isVideo 'true' if incoming call is a video call. Video can be enabled or disabled during a call
		@param {Dictionary} headers Optional SIP headers that was set by a caller
		*/
        public event deligatonIncomingCall onIncomingCall;
		/**
		Called when Voximplant cloud connects audio source to the call. If client previously played a progress tone, it should be stopped
		@event OnCallAudioStarted
		@param {string} callid Call identifier, previously returned by the call() function
		*/
        public event deligateOnCallAudioStarted onCallAudioStarted;
		/**
		Called when Voximplant cloud rejects a call
		@event OnCallFailed
		@param {string} callid Call identifier, previously returned by the call() function
		@param {int} code Status code
		@param {string} reason Text description while call failed
		@param {Dictionary} headers Optional SIP headers that was sent by Voximplant while call was rejected
		*/
        public event deligateOnCallFailed onCallFailed;
		/**
		Called when Voximplant cloud sends RINGING SIP notificatoin via 'call.ring()' method. As response to that event client can play some "ringing" sounds or inform user about "call in progress" some other way
		@event OnCallRinging
		@param {string} callid Call identifier, previously returned by the call() function
		@param {Dictionary} headers Optional SIP headers that was sent by Voximplant as an argument to the 'call.ring()' method call
		*/
        public event deligateOnCallRinging onCallRinging;
		/**
		Called after call is gracefully disconnected from the Voximplant cloud
		@event OnCallDisconnected
		@param {string} callid Call identifier, previously returned by the call() function
		@param {Dictionary} headers Optional SIP headers that was sent by Voximplant while disconnecting the call
		*/
        public event deligateOnCallDisconnected onCallDisconnected;
		/**
		Called after call() method successfully established a call with the Voximplant cloud
		@event OnCallConnected
		@param {string} callid Connected call identifier. It's same identifier returned by the call() function and it can be used in other function to specify one of multiple calls
		@param {Dictionary} headers Optional SIP headers that was sent by Voximplant while accepting the call 
	    */
        public event deligateOnCallConnected onCallConnected;
		/**
		Called if connect() failed to establish a Voximplant cloud connection
		@event onConnectionFailedWithError
		@param {string} error Error message
		*/
        public event deligateOnConnectionFailedWithError onConnectionFailedWithError;
		/**
		Called after connection to Voximplant cloud is closed for any reason
		@event OnConnectionClosed
		*/
        public event deligateOnConnectionClosed onConnectionClosed;
		/**
		Called after connect() successfully connects to Voximplant cloud
		@event OnConnectionSuccessful
		*/
        public event deligateOnConnectionSuccessful onConnectionSuccessful;
		/**
		Called after key requested with 'requestOneTimeKey' is requested and returned from the Voximplant cloud
		@event OnOneTimeKeyGenerated
		@param {string} key Key string that should be used in a hash for the 'loginUsingOneTimeKey'
		*/
        public event deligateOnOneTimeKeyGenerated onOneTimeKeyGenerated;
		/**
		Called if login() call results in the failed login
		@event OnLoginFailed
		@param {string} error Failure reason
		*/
        public event deligateOnLoginFailed onLoginFailed;
		/**
		Called if login() call results in the successful login
		@event OnLoginSuccessful
		@param {string} username Display name of logged in user
		*/
        public event deligateOnLoginSuccessful onLoginSuccessful;
		/**
		Called when you start call
		@event onStartCall
		@param {string} callid Call identifier, previously returned by the call() function
		*/
		public event deligateOnStartCall onStartCall;

		[DllImport ("__Internal")]
		private static extern void iosSDKinit(string pUnityObj);

		[DllImport ("__Internal")]
		private static extern void iosSDKconnect();

		[DllImport ("__Internal")]
		private static extern void iosSDKlogin(string pLogin, string pPass);

		[DllImport ("__Internal", CallingConvention = CallingConvention.Cdecl)]
		private static extern void iosSDKstartCall(string pId, bool pWithVideo, string pCustomData, string pHeaderJson);

		[DllImport ("__Internal")]
		private static extern void iosSDKanswerCall(string pCallId, string pHeaderJson);

		[DllImport ("__Internal")]
		private static extern void iosSDKHungup(string pCallId, string pHeaderJson);

		[DllImport ("__Internal")]
		private static extern void iosSDKDecline(string pCallId, string pHeaderJson);

		[DllImport ("__Internal")]
		private static extern void iosSDKsetMute(bool psetMute);

		[DllImport ("__Internal")]
		private static extern void iosSDKsendVideo(bool psendVideo);

		[DllImport ("__Internal")]
		private static extern void iosSDKsetCamera(bool pSetFront);

		[DllImport ("__Internal")]
		private static extern void iosSDKdisableTls();

		[DllImport ("__Internal")]
		private static extern void iosSDKdisconnectCall(string pCallId, string pHeaderJson);

		[DllImport ("__Internal")]
		private static extern void iosSDKloginUsingOneTimeKey(string pUserName, string pOneTimeKey);

		[DllImport ("__Internal")]
		private static extern void iosSDKrequestOneTimeKey(string pUserName);

		[DllImport ("__Internal")]
		private static extern void iosSDKsendDTFM(string pCallId, int pDigit);

		[DllImport ("__Internal")]
		private static extern void iosSDKsendInfo(string pCallId, string pWithType, string pContent, string pHeaderJson);

		[DllImport ("__Internal")]
		private static extern void iosSDKsendMessage(string pCallId, string pMsg, string pHeaderJson);

		[DllImport ("__Internal")]
		private static extern void iosSDKsetCameraResolution(int pWidth, int pHeight);

		[DllImport ("__Internal")]
		private static extern void iosSDKsetUseLoudspeaker(bool pUseLoudspeaker);

		[DllImport ("__Internal")]
		private static extern void iosSDKCloseConnection();

		[DllImport ("__Internal")]
		private static extern void iosSDKsetLocalSize(int xPos, int yPos, int pWidth, int pHeight);

		[DllImport ("__Internal")]
		private static extern void iosSDKsetRemoteSize(int xPos, int yPos, int pWidth, int pHeight);

        private bool AndroidPlatform()
        {
            return Application.platform == RuntimePlatform.Android;
        }
		private bool IPhonePlatform()
		{
			return Application.platform == RuntimePlatform.IPhonePlayer;
		}

        public void addLog(String pMsg)
        {
            if (LogMethod != null)
                LogMethod(pMsg);
        }

		public void setLocalSizeView(SizeView pSize)
		{
			if (IPhonePlatform())
				iosSDKsetLocalSize(pSize.x_pos, pSize.y_pos, pSize.width, pSize.height);
		}

		public void setRemoteSizeView(SizeView pSize)
		{
			if (IPhonePlatform())
				iosSDKsetRemoteSize(pSize.x_pos, pSize.y_pos, pSize.width, pSize.height);
		}

		public void init(String pObjectNameSDK)
		{
			if (IPhonePlatform())
				iosSDKinit(pObjectNameSDK);
			
			if (AndroidPlatform())
			{
				try
				{
					jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				}
				catch (UnityException e)
				{
					Debug.logger.Log("JC Error: " + e.Message);
				}

				try
				{
					jo = jc.GetStatic<AndroidJavaObject>("currentActivity").Get<AndroidJavaObject>("mVoxClient");
				}
				catch (AndroidJavaException e)
				{
					Debug.logger.Log("JO Error: " + e.Message);
				}
			}
		}

		public void init(String pObjectNameSDK, SizeView pLocalView, SizeView pRemoteView)
		{
			init(pObjectNameSDK);
			if (IPhonePlatform())
			{
				setLocalSizeView(pLocalView);
				setRemoteSizeView(pRemoteView);
			}
		}
			
        /**
        Close connection to the Voximplant cloud that was previously established via 'connect' call
        @method closeConnection
        */
        public void closeConnection()
        {
            if (AndroidPlatform())
                jo.Call("closeConnection");
			if (IPhonePlatform())
				iosSDKCloseConnection();
        }

        /**
        Initiate Voximplant cloud connection. After successful connection a "login" method should be called
        @method connect
        */
        public void connect()
        {
            if (AndroidPlatform())
                jo.Call("connect");
			if (IPhonePlatform())
				iosSDKconnect();
        }
	
		/**
        Login into Voximplant cloud. Should be called after "connect" in the "OnConnectionSuccessful" handler
        @method login
		@param {LoginClassParam} pLogin Username and password. Username is a fully-qualified string that includes Voximplant user, application and account names. The format is: "username@appname.accname.voximplant.com"
        */
        public void login(LoginClassParam pLogin)
        {
            if (AndroidPlatform())
                jo.Call("login", Invoice.JsonUtility.ToJson(pLogin));
			if (IPhonePlatform())
				iosSDKlogin(pLogin.login, pLogin.pass);
        }
	
		/**
		Start a new outgoing call
		@method call
		@param {CallClassParam} pCall Call options: number to call, video call flag and custom data to send alongside the call. For SIP compatibility reasons number should be a non-empty string even if the number itself is not used by a Voximplant cloud scenario. "OnCallConnected" will be called on call success, or "OnCallFailed" will be called if Voximplant cloud rejects a call or network error occur 
		*/
        public void call(CallClassParam pCall) 
        {
            if (AndroidPlatform())
                jo.Call<string>("call", Invoice.JsonUtility.ToJson(pCall));
			if (IPhonePlatform())
				iosSDKstartCall(pCall.userCall, pCall.videoCall, pCall.customData, Invoice.JsonUtility.ToJson(new PairKeyValueArray(pCall.headers)));
        }
	
		/**
		Answer incoming call. Should be called from the "OnIncomingCall" handler
		@method answer
		@param {string} p Call identifier
		@param {Dictionary} (Only for iOS) headers Optional SIP headers set by a info sender
		*/
        public void answer(string pCallId, Dictionary<string, string> pHeader = null)
        {
            if (AndroidPlatform())
                jo.Call("answer", pCallId);
			if (IPhonePlatform())
				iosSDKanswerCall(pCallId, Invoice.JsonUtility.ToJson(new PairKeyValueArray(GetDictionaryToArray(pHeader)))); 
        }
	
		/**
		Decline an incoming call. Should be called from the "OnIncomingCall" handler
		@method declineCall
		@param {string} p Call identifier
		@param {Dictionary} (Only for iOS) headers Optional SIP headers set by a info sender
		*/
        public void declineCall(string pCallId, Dictionary<string, string> pHeader = null)
        {
            if (AndroidPlatform())
                jo.Call("declineCall", pCallId);
			if (IPhonePlatform())
				iosSDKDecline(pCallId, Invoice.JsonUtility.ToJson(new PairKeyValueArray(GetDictionaryToArray(pHeader))));
        }
	
		/**
		Hang up the call in progress. Should be called from the "OnIncomingCall" handler
		@method hangup
		@param {string} p Call identifier
		@param {Dictionary} (Only for iOS) headers Optional SIP headers set by a info sender
		*/
		public void hangup(string pCallId, Dictionary<string, string> pHeader = null)
        {
            if (AndroidPlatform())
				jo.Call("hangup", pCallId);
			if (IPhonePlatform())
				iosSDKHungup(pCallId, Invoice.JsonUtility.ToJson(new PairKeyValueArray(GetDictionaryToArray(pHeader))));
        }
	
		/**
		Mute or unmute microphone. This is reset after audio interruption
		@method setMute
		@param {Boolean} p 'true' to set mute status, 'false' to remove it
		*/
        public void setMute(Boolean p)
        {
            if (AndroidPlatform())
				jo.Call("setMute", Invoice.JsonUtility.ToJson(new BoolClassParam(p)));
			if (IPhonePlatform())
				iosSDKsetMute(p);
        }
	
		/**
		Enable or disable video stream transfer during the call
		@method sendVideo
		@param {Boolean} p 'true' to enable video stream, 'false' to disable it
		*/
        public void sendVideo(Boolean p)
        {
            if (AndroidPlatform())
                jo.Call("sendVideo", Invoice.JsonUtility.ToJson(new BoolClassParam(p)));
			if (IPhonePlatform())
				iosSDKsendVideo(p);
        }
	
		/**
		Select a camera to use for the video call
		@method setCamera
		@param {CameraSet} p A camera to use: 'CameraSet.CAMERA_FACING_FRONT', 'CameraSet.CAMERA_FACING_BACK'
		*/
        public void setCamera(CameraSet p)
        {
            if (AndroidPlatform())
                jo.Call("setCamera", (p == CameraSet.CAMERA_FACING_FRONT)?"1":"0");
			if (IPhonePlatform())
				iosSDKsetCamera((p == CameraSet.CAMERA_FACING_FRONT)?true:false);
        }

		// Internal
        public void disableTls()
        {
            if (AndroidPlatform())
                jo.Call("disableTls");
			if (IPhonePlatform())
				iosSDKdisableTls();
        }
	
		/**
		Disconnect the specified call
		@method disconnectCall
		@param {string} p Call identifier
		@param {Dictionary} (Only for iOS) headers Optional SIP headers set by a info sender
		*/
        public void disconnectCall(string p, Dictionary<string, string> pHeader = null)
        {
            if (AndroidPlatform())
                jo.Call("disconnectCall", Invoice.JsonUtility.ToJson(new StringClassParam(p)));
			if (IPhonePlatform())
				iosSDKdisconnectCall(p, Invoice.JsonUtility.ToJson(new PairKeyValueArray(GetDictionaryToArray(pHeader))));
        }
	
		/**
		If called before any other SDK methods, enables debug logging into target platform default debug log facility
		@method enableDebugLogging
		*/
        public void enableDebugLogging()
        {
            if (AndroidPlatform())
                jo.Call("enableDebugLogging");
			// TODO add enabled on iPhone
        }
	
		/**
		Login using a hash created from one-time key requested via 'requestOneTimeKey' and password. The has can be calculated on your backend so Voximplant password is never used in the application. Hash is calculated as 'MD5(one-time-key + "|" + MD5(vox-user + ":voximplant.com:" + vox-pass))'
		@method loginUsingOneTimeKey
		@param {LoginOneTimeKeyClassParam} pLogin Fully-qualified user name and hash to authenticate. Note that hash is created not from a fully-qualified user name, but from a bare user name, without the "@app.acc.voximplant.com" part
		*/
        public void loginUsingOneTimeKey(LoginOneTimeKeyClassParam pLogin)
        {
            if (AndroidPlatform())
                jo.Call("loginUsingOneTimeKey", Invoice.JsonUtility.ToJson(pLogin));
			if (IPhonePlatform())
				iosSDKloginUsingOneTimeKey(pLogin.name, pLogin.hash);
        }
	
		/**
		Request a one-time key that can be used on your backend to create a login hash for the 'loginUsingOneTimeKey'. Key is returned via 'OnOneTimeKeyGenerated' event
		@method requestOneTimeKey
		@param {string} pName Fully-qualified user name to get a one-time key for. Format is 'user@app.acc.voximplant.com'
		*/
        public void requestOneTimeKey(string pName)
        {
            if (AndroidPlatform())
                jo.Call("requestOneTimeKey", Invoice.JsonUtility.ToJson(new StringClassParam(pName)));
			if (IPhonePlatform())
				iosSDKrequestOneTimeKey(pName);
        }
	
		/**
		Send DTMF signal to the specified call
		@method sendDTMF
		@param {DTFMClassParam} pParam Call identifier and DTMF digit number (0-9, 10 for '*', 11 for '#')
		*/
        public void sendDTMF(DTFMClassParam pParam)
        {
            if (AndroidPlatform())
                jo.Call("sendDTMF", Invoice.JsonUtility.ToJson(pParam));
			if (IPhonePlatform())
				iosSDKsendDTFM(pParam.callId, pParam.digit);
        }
	
		/**
		Send arbitrary data to Voximplant cloud. Data can be received in VoxEngine scenario by subscribing to the 'CallEvents.InfoReceived' event. Optional SIP headers can be automatically passed to second call via VoxEngine 'easyProcess()' method
		@method sendInfo
		@param {InfoClassParam} pParam Call identifier, data MIME type string, data string and optional SIP headers list as an array of 'PairKeyValue' objects with 'key' and 'value' properties
		*/
        public void sendInfo(InfoClassParam pParam)
        {
            if (AndroidPlatform())
                jo.Call("sendInfo", Invoice.JsonUtility.ToJson(pParam));
			if (IPhonePlatform())
				iosSDKsendInfo(pParam.callId,pParam.mimeType,pParam.content, Invoice.JsonUtility.ToJson(new PairKeyValueArray(pParam.headers)));
        }
	
		/**
		Simplified version of 'sendInfo' that uses predefined MIME type to send a text message. Message can be received in VoxEngine scenario by subscribing to the 'CallEvents.MessageReceived' event. Optional SIP headers can be automatically passed to second call via VoxEngine 'easyProcess()' method
		@method sendMessage
		@param {SendMessageClassParam} pParam Call identifier, message string and optional SIP headers list as an array of 'PairKeyValue' objects with 'key' and 'value' properties
		*/
        public void sendMessage(SendMessageClassParam pParam)
        {
            if (AndroidPlatform())
                jo.Call("sendMessage", Invoice.JsonUtility.ToJson(pParam));
			if (IPhonePlatform())
				iosSDKsendMessage(pParam.callId, pParam.text, Invoice.JsonUtility.ToJson(new PairKeyValueArray(pParam.headers)));
        }
	
		/**
		Set local camera resolution. Increasing resolution increases video quality, but also uses more cpu and bandwidth
		@method setCameraResolution
		@param {CameraResolutionClassParam} pParam Camera resolutino as width and height, in pixels
		*/
        public void setCameraResolution(CameraResolutionClassParam pParam)
        {
            if (AndroidPlatform())
                jo.Call("setCameraResolution", Invoice.JsonUtility.ToJson(pParam));
			if (IPhonePlatform())
				iosSDKsetCameraResolution(pParam.width, pParam.height);
        }
	
		/**
		Enable or disable loud speaker, if available
		@method setUseLoudspeaker
		@param {bool} pUseLoudSpeaker 'true' to enable loud speaker, 'false' to disable it
		*/
        public void setUseLoudspeaker(bool pUseLoudSpeaker)
        {
            if (AndroidPlatform())
                jo.Call("setUseLoudspeaker", Invoice.JsonUtility.ToJson(new BoolClassParam(pUseLoudSpeaker)));
			if (IPhonePlatform())
				iosSDKsetUseLoudspeaker(pUseLoudSpeaker);
        }

		// callbacks from Android sdk
        public void faonLoginSuccessful(string p)
        {
            addLog("faonLoginSuccessful: " + p);
            if (onLoginSuccessful != null)
                onLoginSuccessful(p);
        }
        public void faonLoginFailed(string p)
        {
            addLog("faonLoginFailed: " + p);
            if (onLoginFailed != null)
            {
                switch (p)
                {
                    case "INVALID_PASSWORD":
                    {
                        onLoginFailed(LoginFailureReason.INVALID_PASSWORD);
                        break;
                    }
                    case "INVALID_USERNAME":
                    {
                        onLoginFailed(LoginFailureReason.INVALID_USERNAME);
                        break;
                    }
                    case "ACCOUNT_FROZEN":
                    {
                        onLoginFailed(LoginFailureReason.ACCOUNT_FROZEN);
                        break;
                    }
                    case "INTERNAL_ERROR":
                    {
                        onLoginFailed(LoginFailureReason.INTERNAL_ERROR);
                        break;
                    }
                }
            }
                
        }
        public void faonOneTimeKeyGenerated(string p)
        {
            addLog("faonOneTimeKeyGenerated: " + p);
            if (onOneTimeKeyGenerated != null)
                onOneTimeKeyGenerated(p);
        }
        public void faonConnectionSuccessful()
        {
            addLog("faonConnectionSuccessful");
            if (onConnectionSuccessful != null)
                onConnectionSuccessful();
        }
        public void faonConnectionClosed()
        {
            addLog("faonConnectionClosed");
            if (onConnectionClosed != null)
                onConnectionClosed();
        }
        public void faonConnectionFailedWithError(string p)
        {
            addLog("faonConnectionFailedWithError" + p);
            if (onConnectionFailedWithError != null)
                onConnectionFailedWithError(p);
        }
		public void faonCallConnected(string p)
        {
            addLog("faonCallConnected: " + p);
            JSONNode node = GetParamList(p);
            if (onCallConnected != null)
	    	//! 1 arg iOS limit, unpack
                onCallConnected(node[0].Value, node[1].AsDictionary);
        }
        public void faonCallDisconnected(string p)
        {
            addLog("faonCallDisconnected: " + p);
            JSONNode node = GetParamList(p);
            if (onCallDisconnected != null)
                onCallDisconnected(node[0].Value, node[1].AsDictionary);
        }
		public void faonCallRinging(string p)
        {
            addLog("faonCallRinging: " + p);
            JSONNode node = GetParamList(p);
            if (onCallRinging != null)
				onCallRinging(node[0].Value, node[1].AsDictionary);
        }
        public void faonCallFailed(string p)
        {
            addLog("faonCallFailed: " + p);
            JSONNode node = GetParamList(p);
            if (onCallFailed != null)
                onCallFailed(node[0].Value, node[1].AsInt, node[2].Value, node[3].AsDictionary);
        }
        public void faonCallAudioStarted(string p)
        {
            addLog("faonCallAudioStarted" + p);
            if (onCallAudioStarted != null)
                onCallAudioStarted(p);
        }
        public void faonIncomingCall(string p)
        {
            addLog("faonIncomingCall: " + p);
            JSONNode node = GetParamList(p);
            if (onIncomingCall != null)
                onIncomingCall(node[0].Value, node[1].Value, node[2].Value, node[3].AsBool, node[4].AsDictionary);
        }
        public void faonSIPInfoReceivedInCall(string p)
        {
            addLog("faonSIPInfoReceivedInCall");
            JSONNode node = GetParamList(p);
            if (onSIPInfoReceivedInCall != null)
                onSIPInfoReceivedInCall(node[0].Value, node[1].Value, node[2].Value, node[3].AsDictionary);
        }
        public void faonMessageReceivedInCall(string p)
        {
            addLog("faonMessageReceivedInCall");
            JSONNode node = GetParamList(p);
            if (onMessageReceivedInCall != null)
                onMessageReceivedInCall(node[0].Value, node[1].Value, null);
		}
        public void faonNetStatsReceived(string p)
        {
            addLog("faonNetStatsReceived");
            JSONNode node = GetParamList(p);
            if (onNetStatsReceived != null)
                onNetStatsReceived(node[0], node[1].AsInt);
        }

		public void faonOnStartCall(string p)
		{
			addLog("faonOnStartCall: " + p);
			if (onStartCall != null)
				onStartCall(p);
		}

		// callbacks from iOS sdk
		public void fiosonConnectionSuccessful()
		{
			addLog("fiosonConnectionSuccessful");
			if (onConnectionSuccessful != null)
				onConnectionSuccessful();
		}
		public void fiosonConnectionFailedWithError(string p)
		{
			addLog("fiosonConnectionFailedWithError" + p);
			if (onConnectionFailedWithError != null)
				onConnectionFailedWithError(p);
		}
		public void fiosonConnectionClosed()
		{
			addLog("fiosonConnectionClosed");
			if (onConnectionClosed != null)
				onConnectionClosed();
		}
		public void fiosonLoginSuccessful(string p)
		{
			addLog("fiosonLoginSuccessful: " + p);
			if (onLoginSuccessful != null)
				onLoginSuccessful(p);
		}
		public void fiosonLoginFailed(string p)
		{
			addLog("fiosonLoginFailed: " + p);
			if (onLoginFailed != null)
			{
				switch (p)
				{
				case "INVALID_PASSWORD":
					{
						onLoginFailed(LoginFailureReason.INVALID_PASSWORD);
						break;
					}
				case "INVALID_USERNAME":
					{
						onLoginFailed(LoginFailureReason.INVALID_USERNAME);
						break;
					}
				case "ACCOUNT_FROZEN":
					{
						onLoginFailed(LoginFailureReason.ACCOUNT_FROZEN);
						break;
					}
				case "INTERNAL_ERROR":
					{
						onLoginFailed(LoginFailureReason.INTERNAL_ERROR);
						break;
					}
				}
			}
		}
		public void fiosonOneTimeKeyGenerated(string p)
		{
			addLog("fiosonOneTimeKeyGenerated: " + p);
			if (onOneTimeKeyGenerated != null)
				onOneTimeKeyGenerated(p);
		}			
		public void fiosonCallConnected(string p)
		{
			addLog("fiosonCallConnected: " + p);
			JSONNode node = GetParamList(p);
			if (onCallConnected != null)
				onCallConnected(node[0].Value, node[1].AsDictionary);
		}
		public void fiosonCallDisconnected(string p)
		{
			addLog("fiosonCallDisconnected: " + p);
			JSONNode node = GetParamList(p);
			if (onCallDisconnected != null)
				onCallDisconnected(node[0].Value, node[1].AsDictionary);
		}
		public void fiosonCallRinging(string p)
		{
			addLog("fiosonCallRinging: " + p);
			JSONNode node = GetParamList(p);
			if (onCallRinging != null)
				onCallRinging(node[0].Value, node[1].AsDictionary);
		}
		public void fiosonCallFailed(string p)
		{
			addLog("fiosonCallFailed: " + p);
			JSONNode node = GetParamList(p);
			if (onCallFailed != null)
				onCallFailed(node[0].Value, node[1].AsInt, node[2].Value, node[3].AsDictionary);
		}
		public void fiosonCallAudioStarted(string p)
		{
			addLog("fiosonCallAudioStarted" + p);
			if (onCallAudioStarted != null)
				onCallAudioStarted(p);
		}
		public void fiosonIncomingCall(string p)
		{
			addLog("fiosonIncomingCall: " + p);
			JSONNode node = GetParamList(p);
			if (onIncomingCall != null)
				onIncomingCall(node[0].Value, node[1].Value, node[2].Value, node[3].AsBool, node[4].AsDictionary);
		}
		public void fiosonSIPInfoReceivedInCall(string p)
		{
			addLog("fiosonSIPInfoReceivedInCall: " + p);
			JSONNode node = GetParamList(p);
			if (onSIPInfoReceivedInCall != null)
				onSIPInfoReceivedInCall(node[0].Value, node[1].Value, node[2].Value, node[3].AsDictionary);
		}
		public void fiosonMessageReceivedInCall(string p)
		{
			addLog("fiosonMessageReceivedInCall: " + p);
			JSONNode node = GetParamList(p);
			if (onMessageReceivedInCall != null)
				onMessageReceivedInCall(node[0].Value, node[1].Value, node[2].AsDictionary);
		}
		public void fiosonNetStatsReceived(string p)
		{
			addLog("fiosonNetStatsReceived: " + p);
			JSONNode node = GetParamList(p);
			if (onNetStatsReceived != null)
				onNetStatsReceived(node[0].Value, node[1].AsInt);
		}

		public void fiosonOnStartCall(string p)
		{
			addLog("fiosonOnStartCall: " + p);
			if (onStartCall != null)
				onStartCall(p);
		}

        public static JSONNode GetParamList(string p)
        {
            JSONNode rootNode = JSON.Parse(p);
            return rootNode;
        }
	
        public static PairKeyValue[] GetDictionaryToArray(Dictionary<string, string> pDic)
        {
			if (pDic == null)
				return new PairKeyValue[0];

            PairKeyValue[] list = new PairKeyValue[pDic.Count];
            int i = 0;
            foreach (KeyValuePair<string, string> pair in pDic)
            {
                list[i] = new PairKeyValue(pair.Key, pair.Value);
                i += 1;
            }
            return list;
        }
    }
}
