

namespace Murmur
{
    public sealed class ServerCallbackI : ServerCallbackDisp_
    {
        public override void channelCreated(Murmur.Channel state, Ice.Current current__)
        {
        }

        public override void channelRemoved(Murmur.Channel state, Ice.Current current__)
        {
        }

        public override void channelStateChanged(Murmur.Channel state, Ice.Current current__)
        {
        }

        public override void userConnected(Murmur.User state, Ice.Current current__)
        {
        }

        public override void userDisconnected(Murmur.User state, Ice.Current current__)
        {
        }

        public override void userStateChanged(Murmur.User state, Ice.Current current__)
        {
        }
    }

    public sealed class ServerContextCallbackI : ServerContextCallbackDisp_
    {
        public override void contextAction(string action, Murmur.User usr, int session, int channelid, Ice.Current current__)
        {
        }
    }

    public sealed class ServerAuthenticatorI : ServerAuthenticatorDisp_
    {
        public override int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, Ice.Current current__)
        {
            newname = null;
            groups = null;
            return 0;
        }

        public override bool getInfo(int id, out System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__)
        {
            info = null;
            return false;
        }

        public override string idToName(int id, Ice.Current current__)
        {
            return null;
        }

        public override byte[] idToTexture(int id, Ice.Current current__)
        {
            return null;
        }

        public override int nameToId(string name, Ice.Current current__)
        {
            return 0;
        }
    }

    public sealed class ServerUpdatingAuthenticatorI : ServerUpdatingAuthenticatorDisp_
    {
        public override int authenticate(string name, string pw, byte[][] certificates, string certhash, bool certstrong, out string newname, out string[] groups, Ice.Current current__)
        {
            newname = null;
            groups = null;
            return 0;
        }

        public override bool getInfo(int id, out System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__)
        {
            info = null;
            return false;
        }

        public override string idToName(int id, Ice.Current current__)
        {
            return null;
        }

        public override byte[] idToTexture(int id, Ice.Current current__)
        {
            return null;
        }

        public override int nameToId(string name, Ice.Current current__)
        {
            return 0;
        }

        public override System.Collections.Generic.Dictionary<int, string> getRegisteredUsers(string filter, Ice.Current current__)
        {
            return null;
        }

        public override int registerUser(System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__)
        {
            return 0;
        }

        public override int setInfo(int id, System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__)
        {
            return 0;
        }

        public override int setTexture(int id, byte[] tex, Ice.Current current__)
        {
            return 0;
        }

        public override int unregisterUser(int id, Ice.Current current__)
        {
            return 0;
        }
    }

    public sealed class ServerI : ServerDisp_
    {
        public override void addCallback_async(Murmur.AMD_Server_addCallback cb__, Murmur.ServerCallbackPrx cb, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void addChannel_async(Murmur.AMD_Server_addChannel cb__, string name, int parent, Ice.Current current__)
        {
            int ret__ = 0;
            cb__.ice_response(ret__);
        }

        public override void addContextCallback_async(Murmur.AMD_Server_addContextCallback cb__, int session, string action, string text, Murmur.ServerContextCallbackPrx cb, int ctx, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void addUserToGroup_async(Murmur.AMD_Server_addUserToGroup cb__, int channelid, int session, string group, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void delete_async(Murmur.AMD_Server_delete cb__, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void getACL_async(Murmur.AMD_Server_getACL cb__, int channelid, Ice.Current current__)
        {
            Murmur.ACL[] acls = null;
            Murmur.Group[] groups = null;
            bool inherit = false;
            cb__.ice_response(acls, groups, inherit);
        }

        public override void getAllConf_async(Murmur.AMD_Server_getAllConf cb__, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<string, string> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getBans_async(Murmur.AMD_Server_getBans cb__, Ice.Current current__)
        {
            Murmur.Ban[] ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getCertificateList_async(Murmur.AMD_Server_getCertificateList cb__, int session, Ice.Current current__)
        {
            byte[][] ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getChannelState_async(Murmur.AMD_Server_getChannelState cb__, int channelid, Ice.Current current__)
        {
            Murmur.Channel ret__ = new Murmur.Channel();
            cb__.ice_response(ret__);
        }

        public override void getChannels_async(Murmur.AMD_Server_getChannels cb__, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<int, Murmur.Channel> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getConf_async(Murmur.AMD_Server_getConf cb__, string key, Ice.Current current__)
        {
            string ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getLog_async(Murmur.AMD_Server_getLog cb__, int first, int last, Ice.Current current__)
        {
            Murmur.LogEntry[] ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getLogLen_async(Murmur.AMD_Server_getLogLen cb__, Ice.Current current__)
        {
            int ret__ = 0;
            cb__.ice_response(ret__);
        }

        public override void getRegisteredUsers_async(Murmur.AMD_Server_getRegisteredUsers cb__, string filter, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<int, string> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getRegistration_async(Murmur.AMD_Server_getRegistration cb__, int userid, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<Murmur.UserInfo, string> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getState_async(Murmur.AMD_Server_getState cb__, int session, Ice.Current current__)
        {
            Murmur.User ret__ = new Murmur.User();
            cb__.ice_response(ret__);
        }

        public override void getTexture_async(Murmur.AMD_Server_getTexture cb__, int userid, Ice.Current current__)
        {
            byte[] ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getTree_async(Murmur.AMD_Server_getTree cb__, Ice.Current current__)
        {
            Murmur.Tree ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getUptime_async(Murmur.AMD_Server_getUptime cb__, Ice.Current current__)
        {
            int ret__ = 0;
            cb__.ice_response(ret__);
        }

        public override void getUserIds_async(Murmur.AMD_Server_getUserIds cb__, string[] names, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<string, int> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getUserNames_async(Murmur.AMD_Server_getUserNames cb__, int[] ids, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<int, string> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getUsers_async(Murmur.AMD_Server_getUsers cb__, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<int, Murmur.User> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void hasPermission_async(Murmur.AMD_Server_hasPermission cb__, int session, int channelid, int perm, Ice.Current current__)
        {
            bool ret__ = false;
            cb__.ice_response(ret__);
        }

        public override void id_async(Murmur.AMD_Server_id cb__, Ice.Current current__)
        {
            int ret__ = 0;
            cb__.ice_response(ret__);
        }

        public override void isRunning_async(Murmur.AMD_Server_isRunning cb__, Ice.Current current__)
        {
            bool ret__ = false;
            cb__.ice_response(ret__);
        }

        public override void kickUser_async(Murmur.AMD_Server_kickUser cb__, int session, string reason, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void redirectWhisperGroup_async(Murmur.AMD_Server_redirectWhisperGroup cb__, int session, string source, string target, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void registerUser_async(Murmur.AMD_Server_registerUser cb__, System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__)
        {
            int ret__ = 0;
            cb__.ice_response(ret__);
        }

        public override void removeCallback_async(Murmur.AMD_Server_removeCallback cb__, Murmur.ServerCallbackPrx cb, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void removeChannel_async(Murmur.AMD_Server_removeChannel cb__, int channelid, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void removeContextCallback_async(Murmur.AMD_Server_removeContextCallback cb__, Murmur.ServerContextCallbackPrx cb, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void removeUserFromGroup_async(Murmur.AMD_Server_removeUserFromGroup cb__, int channelid, int session, string group, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void sendMessage_async(Murmur.AMD_Server_sendMessage cb__, int session, string text, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void sendMessageChannel_async(Murmur.AMD_Server_sendMessageChannel cb__, int channelid, bool tree, string text, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void setACL_async(Murmur.AMD_Server_setACL cb__, int channelid, Murmur.ACL[] acls, Murmur.Group[] groups, bool inherit, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void setAuthenticator_async(Murmur.AMD_Server_setAuthenticator cb__, Murmur.ServerAuthenticatorPrx auth, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void setBans_async(Murmur.AMD_Server_setBans cb__, Murmur.Ban[] bans, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void setChannelState_async(Murmur.AMD_Server_setChannelState cb__, Murmur.Channel state, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void setConf_async(Murmur.AMD_Server_setConf cb__, string key, string value, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void setState_async(Murmur.AMD_Server_setState cb__, Murmur.User state, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void setSuperuserPassword_async(Murmur.AMD_Server_setSuperuserPassword cb__, string pw, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void setTexture_async(Murmur.AMD_Server_setTexture cb__, int userid, byte[] tex, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void start_async(Murmur.AMD_Server_start cb__, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void stop_async(Murmur.AMD_Server_stop cb__, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void unregisterUser_async(Murmur.AMD_Server_unregisterUser cb__, int userid, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void updateRegistration_async(Murmur.AMD_Server_updateRegistration cb__, int userid, System.Collections.Generic.Dictionary<Murmur.UserInfo, string> info, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void verifyPassword_async(Murmur.AMD_Server_verifyPassword cb__, string name, string pw, Ice.Current current__)
        {
            int ret__ = 0;
            cb__.ice_response(ret__);
        }
    }

    public sealed class MetaCallbackI : MetaCallbackDisp_
    {
        public override void started(Murmur.ServerPrx srv, Ice.Current current__)
        {
        }

        public override void stopped(Murmur.ServerPrx srv, Ice.Current current__)
        {
        }
    }

    public sealed class MetaI : MetaDisp_
    {
        public override void addCallback_async(Murmur.AMD_Meta_addCallback cb__, Murmur.MetaCallbackPrx cb, Ice.Current current__)
        {
            cb__.ice_response();
        }

        public override void getAllServers_async(Murmur.AMD_Meta_getAllServers cb__, Ice.Current current__)
        {
            Murmur.ServerPrx[] ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getBootedServers_async(Murmur.AMD_Meta_getBootedServers cb__, Ice.Current current__)
        {
            Murmur.ServerPrx[] ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getDefaultConf_async(Murmur.AMD_Meta_getDefaultConf cb__, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<string, string> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getServer_async(Murmur.AMD_Meta_getServer cb__, int id, Ice.Current current__)
        {
            Murmur.ServerPrx ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getSlice_async(Murmur.AMD_Meta_getSlice cb__, Ice.Current current__)
        {
            string ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getSliceChecksums_async(Murmur.AMD_Meta_getSliceChecksums cb__, Ice.Current current__)
        {
            System.Collections.Generic.Dictionary<string, string> ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void getUptime_async(Murmur.AMD_Meta_getUptime cb__, Ice.Current current__)
        {
            int ret__ = 0;
            cb__.ice_response(ret__);
        }

        public override void getVersion_async(Murmur.AMD_Meta_getVersion cb__, Ice.Current current__)
        {
            int major = 0;
            int minor = 0;
            int patch = 0;
            string text = null;
            cb__.ice_response(major, minor, patch, text);
        }

        public override void newServer_async(Murmur.AMD_Meta_newServer cb__, Ice.Current current__)
        {
            Murmur.ServerPrx ret__ = null;
            cb__.ice_response(ret__);
        }

        public override void removeCallback_async(Murmur.AMD_Meta_removeCallback cb__, Murmur.MetaCallbackPrx cb, Ice.Current current__)
        {
            cb__.ice_response();
        }
    }
}
