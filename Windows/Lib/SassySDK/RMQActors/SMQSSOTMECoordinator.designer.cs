
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using SassyMQ.Lib.RabbitMQ;
using SassyMQ.SSOTME.Lib.RabbitMQ;
using SassyMQ.Lib.RabbitMQ.Payload;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SassyMQ.SSOTME.Lib;
using CoreLibrary.SassyMQ;

namespace SassyMQ.SSOTME.Lib.RMQActors
{
    public partial class SMQSSOTMECoordinator : SSOTMEActorBase
    {
     
        public SMQSSOTMECoordinator(bool isAutoConnect = true)
            : base("ssotmecoordinator.all", isAutoConnect)
        {
        }
        // SSoTmeOST - SSOTME
        public virtual bool Connect(string virtualHost, string username, string password)
        {
            return base.Connect(virtualHost, username, password);
        }   

        protected override void CheckRouting(SSOTMEPayload payload) 
        {
            this.CheckRouting(payload, false);
        }

        partial void CheckPayload(SSOTMEPayload payload);

        private void Reply(SSOTMEPayload payload)
        {
            if (!System.String.IsNullOrEmpty(payload.ReplyTo))
            {
                payload.DirectMessageQueue = this.QueueName;
                this.CheckPayload(payload);
                this.RMQChannel.BasicPublish("", payload.ReplyTo, body: Encoding.UTF8.GetBytes(payload.ToJSonString()));
            }
        }

        protected override void CheckRouting(SSOTMEPayload payload, bool isDirectMessage) 
        {
            // if (payload.IsDirectMessage && !isDirectMessage) return;

            
             if (payload.IsLexiconTerm(LexiconTermEnum.publicuser_ping_ssotmecoordinator)) 
            {
                this.OnPublicUserPingReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.publicuser_register_ssotmecoordinator)) 
            {
                this.OnPublicUserRegisterReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.publicuser_recover_ssotmecoordinator)) 
            {
                this.OnPublicUserRecoverReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.publicuser_getalltranspilers_ssotmecoordinator)) 
            {
                this.OnPublicUserGetAllTranspilersReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.publicuser_getallplatformdata_ssotmecoordinator)) 
            {
                this.OnPublicUserGetAllPlatformDataReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.publicuser_getallfiletypes_ssotmecoordinator)) 
            {
                this.OnPublicUserGetAllFileTypesReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_ping_ssotmecoordinator)) 
            {
                this.OnAccountHolderPingReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_login_ssotmecoordinator)) 
            {
                this.OnAccountHolderLoginReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_logout_ssotmecoordinator)) 
            {
                this.OnAccountHolderLogoutReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_addtranspiler_ssotmecoordinator)) 
            {
                this.OnAccountHolderAddTranspilerReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_deletetranspiler_ssotmecoordinator)) 
            {
                this.OnAccountHolderDeleteTranspilerReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_updatetranspiler_ssotmecoordinator)) 
            {
                this.OnAccountHolderUpdateTranspilerReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_gettranspiler_ssotmecoordinator)) 
            {
                this.OnAccountHolderGetTranspilerReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_addtranspilerversion_ssotmecoordinator)) 
            {
                this.OnAccountHolderAddTranspilerVersionReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_deletetranspilerversion_ssotmecoordinator)) 
            {
                this.OnAccountHolderDeleteTranspilerVersionReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_updatetranspilerversion_ssotmecoordinator)) 
            {
                this.OnAccountHolderUpdateTranspilerVersionReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_gettranspilerlist_ssotmecoordinator)) 
            {
                this.OnAccountHolderGetTranspilerListReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_createproject_ssotmecoordinator)) 
            {
                this.OnAccountHolderCreateProjectReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_requesttranspile_ssotmecoordinator)) 
            {
                this.OnAccountHolderRequestTranspileReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_requesttranspilerhost_ssotmecoordinator)) 
            {
                this.OnAccountHolderRequestTranspilerHostReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_requeststopinstance_ssotmecoordinator)) 
            {
                this.OnAccountHolderRequestStopInstanceReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_requeststophost_ssotmecoordinator)) 
            {
                this.OnAccountHolderRequestStopHostReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.accountholder_commandlinetranspile_ssotmecoordinator)) 
            {
                this.OnAccountHolderCommandLineTranspileReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.transpilerhost_ping_ssotmecoordinator)) 
            {
                this.OnTranspilerHostPingReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.transpilerhost_offline_ssotmecoordinator)) 
            {
                this.OnTranspilerHostOfflineReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.transpilerhost_instancestarted_ssotmecoordinator)) 
            {
                this.OnTranspilerHostInstanceStartedReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.transpilerhost_instancestopped_ssotmecoordinator)) 
            {
                this.OnTranspilerHostInstanceStoppedReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.ssotmeadmin_ping_ssotmecoordinator)) 
            {
                this.OnSSOTMEAdminPingReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.ssotmeadmin_addplatformcategory_ssotmecoordinator)) 
            {
                this.OnSSOTMEAdminAddPlatformCategoryReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.ssotmeadmin_updateplatformcategory_ssotmecoordinator)) 
            {
                this.OnSSOTMEAdminUpdatePlatformCategoryReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.ssotmeadmin_addtranspilerplatform_ssotmecoordinator)) 
            {
                this.OnSSOTMEAdminAddTranspilerPlatformReceived(payload);
                this.Reply(payload);
            }
        
            else  if (payload.IsLexiconTerm(LexiconTermEnum.ssotmeadmin_updatetranspilerplatform_ssotmecoordinator)) 
            {
                this.OnSSOTMEAdminUpdateTranspilerPlatformReceived(payload);
                this.Reply(payload);
            }
        
        }

        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> PublicUserPingReceived;
        protected virtual void OnPublicUserPingReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Ping - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.PublicUserPingReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> PublicUserRegisterReceived;
        protected virtual void OnPublicUserRegisterReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Register - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.PublicUserRegisterReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> PublicUserRecoverReceived;
        protected virtual void OnPublicUserRecoverReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Recover - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.PublicUserRecoverReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> PublicUserGetAllTranspilersReceived;
        protected virtual void OnPublicUserGetAllTranspilersReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Get All Transpilers - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.PublicUserGetAllTranspilersReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> PublicUserGetAllPlatformDataReceived;
        protected virtual void OnPublicUserGetAllPlatformDataReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Get All Platform Data - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.PublicUserGetAllPlatformDataReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> PublicUserGetAllFileTypesReceived;
        protected virtual void OnPublicUserGetAllFileTypesReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Get All File Types - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.PublicUserGetAllFileTypesReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderPingReceived;
        protected virtual void OnAccountHolderPingReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Ping - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderPingReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderLoginReceived;
        protected virtual void OnAccountHolderLoginReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Login - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderLoginReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderLogoutReceived;
        protected virtual void OnAccountHolderLogoutReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Logout - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderLogoutReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderAddTranspilerReceived;
        protected virtual void OnAccountHolderAddTranspilerReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Add Transpiler - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderAddTranspilerReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderDeleteTranspilerReceived;
        protected virtual void OnAccountHolderDeleteTranspilerReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Delete Transpiler - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderDeleteTranspilerReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderUpdateTranspilerReceived;
        protected virtual void OnAccountHolderUpdateTranspilerReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Update Transpiler - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderUpdateTranspilerReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderGetTranspilerReceived;
        protected virtual void OnAccountHolderGetTranspilerReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Get Transpiler - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderGetTranspilerReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderAddTranspilerVersionReceived;
        protected virtual void OnAccountHolderAddTranspilerVersionReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Add Transpiler Version - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderAddTranspilerVersionReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderDeleteTranspilerVersionReceived;
        protected virtual void OnAccountHolderDeleteTranspilerVersionReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Delete Transpiler Version - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderDeleteTranspilerVersionReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderUpdateTranspilerVersionReceived;
        protected virtual void OnAccountHolderUpdateTranspilerVersionReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Update Transpiler Version - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderUpdateTranspilerVersionReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderGetTranspilerListReceived;
        protected virtual void OnAccountHolderGetTranspilerListReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Get Transpiler List - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderGetTranspilerListReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderCreateProjectReceived;
        protected virtual void OnAccountHolderCreateProjectReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Create Project - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderCreateProjectReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderRequestTranspileReceived;
        protected virtual void OnAccountHolderRequestTranspileReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Request Transpile - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderRequestTranspileReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderRequestTranspilerHostReceived;
        protected virtual void OnAccountHolderRequestTranspilerHostReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Request Transpiler Host - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderRequestTranspilerHostReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderRequestStopInstanceReceived;
        protected virtual void OnAccountHolderRequestStopInstanceReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Request Stop Instance - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderRequestStopInstanceReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderRequestStopHostReceived;
        protected virtual void OnAccountHolderRequestStopHostReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Request Stop Host - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderRequestStopHostReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> AccountHolderCommandLineTranspileReceived;
        protected virtual void OnAccountHolderCommandLineTranspileReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Command Line Transpile - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.AccountHolderCommandLineTranspileReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> TranspilerHostPingReceived;
        protected virtual void OnTranspilerHostPingReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Ping - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.TranspilerHostPingReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> TranspilerHostOfflineReceived;
        protected virtual void OnTranspilerHostOfflineReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Offline - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.TranspilerHostOfflineReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> TranspilerHostInstanceStartedReceived;
        protected virtual void OnTranspilerHostInstanceStartedReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Instance Started - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.TranspilerHostInstanceStartedReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> TranspilerHostInstanceStoppedReceived;
        protected virtual void OnTranspilerHostInstanceStoppedReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Instance Stopped - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.TranspilerHostInstanceStoppedReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> SSOTMEAdminPingReceived;
        protected virtual void OnSSOTMEAdminPingReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Ping - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.SSOTMEAdminPingReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> SSOTMEAdminAddPlatformCategoryReceived;
        protected virtual void OnSSOTMEAdminAddPlatformCategoryReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Add Platform Category - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.SSOTMEAdminAddPlatformCategoryReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> SSOTMEAdminUpdatePlatformCategoryReceived;
        protected virtual void OnSSOTMEAdminUpdatePlatformCategoryReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Update Platform Category - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.SSOTMEAdminUpdatePlatformCategoryReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> SSOTMEAdminAddTranspilerPlatformReceived;
        protected virtual void OnSSOTMEAdminAddTranspilerPlatformReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Add Transpiler Platform - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.SSOTMEAdminAddTranspilerPlatformReceived, plea);
        }
        
        public event System.EventHandler<PayloadEventArgs<SSOTMEPayload>> SSOTMEAdminUpdateTranspilerPlatformReceived;
        protected virtual void OnSSOTMEAdminUpdateTranspilerPlatformReceived(SSOTMEPayload payload)
        {
            if (IsDebugMode) 
            {
                System.Console.WriteLine("Update Transpiler Platform - ");
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var plea = new PayloadEventArgs<SSOTMEPayload>(payload);
            this.Invoke(this.SSOTMEAdminUpdateTranspilerPlatformReceived, plea);
        }
        
            public void SSOTMECoordinatorGetInstances(DMProxy proxy) {
            this.SSOTMECoordinatorGetInstances(this.CreatePayload(), proxy);
            }

            public void SSOTMECoordinatorGetInstances(System.String content, DMProxy proxy) {
            var payload = this.CreatePayload();
            payload.Content = content;
            this.SSOTMECoordinatorGetInstances(payload, proxy);
            }

            public void SSOTMECoordinatorGetInstances(SSOTMEPayload payload, DMProxy proxy)
            {
            payload.IsDirectMessage = true;
            this.SendMessage(payload, "Get Instances - ",
            "ssotmecoordinatormic", "transpilerhost.general.ssotmecoordinator.getinstances", proxy.RoutingKey);
            }


        
            public void SSOTMECoordinatorTranspileRequested(DMProxy proxy) {
            this.SSOTMECoordinatorTranspileRequested(this.CreatePayload(), proxy);
            }

            public void SSOTMECoordinatorTranspileRequested(System.String content, DMProxy proxy) {
            var payload = this.CreatePayload();
            payload.Content = content;
            this.SSOTMECoordinatorTranspileRequested(payload, proxy);
            }

            public void SSOTMECoordinatorTranspileRequested(SSOTMEPayload payload, DMProxy proxy)
            {
            payload.IsDirectMessage = true;
            this.SendMessage(payload, "Transpile Requested - ",
            "ssotmecoordinatormic", "transpilerhost.general.ssotmecoordinator.transpilerequested", proxy.RoutingKey);
            }


        
            public void SSOTMECoordinatorTranspilerOnline() {
            this.SSOTMECoordinatorTranspilerOnline(this.CreatePayload());
            }

            public void SSOTMECoordinatorTranspilerOnline(System.String content) {
            var payload = this.CreatePayload();
            payload.Content = content;
            this.SSOTMECoordinatorTranspilerOnline(payload);
            }

            public void SSOTMECoordinatorTranspilerOnline(SSOTMEPayload payload)
            {
            
            this.SendMessage(payload, "Transpiler Online - ",
            "ssotmecoordinatormic", "publicuser.general.ssotmecoordinator.transpileronline");
            }


        
            public void SSOTMECoordinatorTranspilerOffline() {
            this.SSOTMECoordinatorTranspilerOffline(this.CreatePayload());
            }

            public void SSOTMECoordinatorTranspilerOffline(System.String content) {
            var payload = this.CreatePayload();
            payload.Content = content;
            this.SSOTMECoordinatorTranspilerOffline(payload);
            }

            public void SSOTMECoordinatorTranspilerOffline(SSOTMEPayload payload)
            {
            
            this.SendMessage(payload, "Transpiler Offline - ",
            "ssotmecoordinatormic", "publicuser.general.ssotmecoordinator.transpileroffline");
            }


        
            public void SSOTMECoordinatorStopInstance(DMProxy proxy) {
            this.SSOTMECoordinatorStopInstance(this.CreatePayload(), proxy);
            }

            public void SSOTMECoordinatorStopInstance(System.String content, DMProxy proxy) {
            var payload = this.CreatePayload();
            payload.Content = content;
            this.SSOTMECoordinatorStopInstance(payload, proxy);
            }

            public void SSOTMECoordinatorStopInstance(SSOTMEPayload payload, DMProxy proxy)
            {
            payload.IsDirectMessage = true;
            this.SendMessage(payload, "Stop Instance - ",
            "ssotmecoordinatormic", "transpilerhost.general.ssotmecoordinator.stopinstance", proxy.RoutingKey);
            }


        
            public void SSOTMECoordinatorStopHost(DMProxy proxy) {
            this.SSOTMECoordinatorStopHost(this.CreatePayload(), proxy);
            }

            public void SSOTMECoordinatorStopHost(System.String content, DMProxy proxy) {
            var payload = this.CreatePayload();
            payload.Content = content;
            this.SSOTMECoordinatorStopHost(payload, proxy);
            }

            public void SSOTMECoordinatorStopHost(SSOTMEPayload payload, DMProxy proxy)
            {
            payload.IsDirectMessage = true;
            this.SendMessage(payload, "Stop Host - ",
            "ssotmecoordinatormic", "transpilerhost.general.ssotmecoordinator.stophost", proxy.RoutingKey);
            }


        
    }
}

                    