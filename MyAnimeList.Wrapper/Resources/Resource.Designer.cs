﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MyAnimeList.Wrapper.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resource {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resource() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MyAnimeList.Wrapper.Resources.Resource", typeof(Resource).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Result not found.
        /// </summary>
        internal static string NoContentException {
            get {
                return ResourceManager.GetString("NoContentException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You&apos;re not authenticated.
        ///Please verify your login/password..
        /// </summary>
        internal static string NotAuthenticated {
            get {
                return ResourceManager.GetString("NotAuthenticated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bad HTTP request.
        ///Please contact the support..
        /// </summary>
        internal static string ServiceBadRequestException {
            get {
                return ResourceManager.GetString("ServiceBadRequestException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service did not respond (HTTP code: {0}).
        /// </summary>
        internal static string ServiceDidNotRespondException {
            get {
                return ResourceManager.GetString("ServiceDidNotRespondException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server busy (HTTP code: {0}).
        ///Please retry later..
        /// </summary>
        internal static string ServiceServerBusyException {
            get {
                return ResourceManager.GetString("ServiceServerBusyException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connection to the server failed (HTTP code: {0}).
        ///Please check your settings..
        /// </summary>
        internal static string ServiceServerErrorConnectionException {
            get {
                return ResourceManager.GetString("ServiceServerErrorConnectionException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server maintenance (HTTP code: {0}).
        /// </summary>
        internal static string ServiceServerMaintenanceException {
            get {
                return ResourceManager.GetString("ServiceServerMaintenanceException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to perform the action.
        ///Please try again later or contact the support..
        /// </summary>
        internal static string ServiceUnableToPerformActionException {
            get {
                return ResourceManager.GetString("ServiceUnableToPerformActionException", resourceCulture);
            }
        }
    }
}
