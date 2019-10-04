// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Commands.ResourceManager.Common.Tags;
using System.Net;
using Microsoft.Azure.Commands.ResourceManager.Common.ArgumentCompleters;
using Microsoft.Azure.Commands.Network.Models;
using Microsoft.Azure.Management.Internal.Resources.Utilities.Models;
using Microsoft.Azure.Management.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using MNM = Microsoft.Azure.Management.Network.Models;
using System.Linq;
using CNM = Microsoft.Azure.Commands.Network.Models;
using Microsoft.Azure.Commands.Common.Strategies;
using Microsoft.Azure.Management.Network.Models;

namespace Microsoft.Azure.Commands.Network
{
    [Cmdlet(VerbsCommon.Add, ResourceManager.Common.AzureRMConstants.AzureRMPrefix + "VirtualRouterPeer", SupportsShouldProcess = true, DefaultParameterSetName = VirtualRouterPeerParameterSetNames.ByVirtualRouterName), OutputType(typeof(PSVirtualRouter))]
    public partial class AddAzureRmVirtualRouterPeer : NetworkBaseCmdlet
    {
        [Parameter(
            Mandatory = true,
            HelpMessage = "The resource group name of the virtual router/peer.",
            ValueFromPipelineByPropertyName = true)]
        [ResourceGroupCompleter]
        [ValidateNotNullOrEmpty]
        public string ResourceGroupName { get; set; }

        [Alias("ResourceName")]
        [Parameter(
            Mandatory = true,
            HelpMessage = "The name of the virtual router Peer.",
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string PeerName { get; set; }

        [Parameter(
            Mandatory = true,
            HelpMessage = "Peer Ip.",
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string PeerIp { get; set; }

        [Parameter(
            Mandatory = true,
            HelpMessage = "Peer ASN.",
            ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public uint PeerAsn { get; set; }

        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = VirtualRouterPeerParameterSetNames.ByVirtualRouterName,
            HelpMessage = "The virtual router where peer exists.")]
        public string VirtualRouterName { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Do not ask for confirmation if you want to overwrite a resource")]
        public SwitchParameter Force { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Run cmdlet in the background")]
        public SwitchParameter AsJob { get; set; }

        public override void Execute()
        {
            base.Execute();

            var present = true;
            try
            {
                this.NetworkClient.NetworkManagementClient.VirtualRouterPeerings.Get(this.ResourceGroupName, this.VirtualRouterName, this.PeerName);
            }
            catch (Exception ex)
            {
                if (ex is Microsoft.Azure.Management.Network.Models.ErrorException || ex is Rest.Azure.CloudException)
                {
                    // Resource is not present
                    present = false;
                }
                else
                {
                    throw;
                }
            }

            if (present)
            {
                throw new PSArgumentException(string.Format(Properties.Resources.ResourceAlreadyPresentInResourceGroup, this.PeerName, this.ResourceGroupName, this.VirtualRouterName));
            }

            ConfirmAction(
                Properties.Resources.CreatingResourceMessage,
                PeerName,
                () =>
                {
                    WriteVerbose(String.Format(Properties.Resources.CreatingLongRunningOperationMessage, this.ResourceGroupName, this.VirtualRouterName, this.PeerName));
                    PSVirtualRouterPeer virtualRouterPeer = new PSVirtualRouterPeer
                    {
                        PeerAsn = this.PeerAsn,
                        PeerIp = this.PeerIp,
                        Name = this.PeerName
                    };

                    var vVirtualRouterPeerModel = NetworkResourceManagerProfile.Mapper.Map<MNM.VirtualRouterPeering>(virtualRouterPeer);

                    this.NetworkClient.NetworkManagementClient.VirtualRouterPeerings.CreateOrUpdate(this.ResourceGroupName, this.VirtualRouterName, this.PeerName, vVirtualRouterPeerModel);
                    var getVirtualRouter = this.NetworkClient.NetworkManagementClient.VirtualRouters.Get(this.ResourceGroupName, this.VirtualRouterName);
                    var vVirtualRouterModel = NetworkResourceManagerProfile.Mapper.Map<CNM.PSVirtualRouter>(getVirtualRouter);
                    vVirtualRouterModel.ResourceGroupName = this.ResourceGroupName;
                    vVirtualRouterModel.Tag = TagsConversionHelper.CreateTagHashtable(getVirtualRouter.Tags);
                    if (getVirtualRouter.Peerings != null && getVirtualRouter.Peerings.Count > 0)
                    {
                        var vVirtualRouterPeering = this.NetworkClient.NetworkManagementClient.VirtualRouterPeerings.List(ResourceGroupName, this.VirtualRouterName);
                        var vVirtualRouterPeeringList = ListNextLink<VirtualRouterPeering>.GetAllResourcesByPollingNextLink(vVirtualRouterPeering, this.NetworkClient.NetworkManagementClient.VirtualRouterPeerings.ListNext);
                        vVirtualRouterModel.Peerings.Add(NetworkResourceManagerProfile.Mapper.Map<CNM.PSVirtualRouterPeer>(vVirtualRouterPeeringList));
                    }
                    WriteObject(vVirtualRouterModel, true);
                });

        }
    }
}
