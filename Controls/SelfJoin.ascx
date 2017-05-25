<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SelfJoin.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.SelfJoin.SelfJoin" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Warning" />
        <Rock:NotificationBox ID="nbErrorMessage" runat="server" NotificationBoxType="Danger" />
        <Rock:NotificationBox ID="nbSuccessMessage" runat="server" NotificationBoxType="Success" />

        <asp:Panel ID="pnlGroupList" CssClass="panel panel-block" runat="server">
            <div class="panel-body">
                <asp:HiddenField ID="hfSelection" runat="server" />
                <asp:Panel ID="pnlContent" runat="server">
                    <asp:Literal ID="ltContent" runat="server" />
                </asp:Panel>
            </div>

            <div class="panel-footer">
                <asp:Button ID="btnSubmit" runat="server" CssClass="btn btn-primary" Text="Save" OnClick="btnSubmit_Click" />
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlLavaDebug" runat="server" Visible="false">
            <asp:Literal ID="ltLavaDebug" runat="server" />
        </asp:Panel>

        <asp:Panel ID="pnlGroupAttributes" runat="server" Visible="false">
            <asp:Repeater ID="rptrGroupAttributes" runat="server" OnItemDataBound="rptrGroupAttributes_ItemDataBound">
                <ItemTemplate>
                    <h3>Information needed for <%# Eval("Group.Name") %></h3>
                    <asp:PlaceHolder ID="phAttributes" runat="server" />
                </ItemTemplate>
            </asp:Repeater>

            <asp:Button ID="btnAttributesBack" runat="server" CssClass="btn btn-default" Text="Back" OnClick="btnAttributesBack_Click" CausesValidation="false" />
            <asp:Button ID="btnAttributesSubmit" runat="server" CssClass="btn btn-primary" Text="Submit" OnClick="btnAttributesSubmit_Click" />
        </asp:Panel>

        <asp:Panel ID="pnlSettingsModal" runat="server" Visible="false">
            <Rock:ModalDialog ID="mdSettings" runat="server" OnSaveClick="lbSettingsSave_Click" Title="Settings">
                <Content>
                    <asp:UpdatePanel ID="upnlSettings" runat="server">
                        <ContentTemplate>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="panel panel-default">
                                        <div class="panel-heading">
                                            <h3 class="panel-title">General</h3>
                                        </div>
                                        <div class="panel-body">
                                            <Rock:GroupPicker ID="gpSettingsGroup" runat="server" Label="Group" Help="The group to use as a root for showing selections. This is passed to the Lava parser as a property called Group and can be used to build the list of checkboxes or radio buttons." Required="true" OnSelectItem="gpSettingsGroup_SelectItem" />
                                            <Rock:GroupRolePicker ID="grpSettingsRole" runat="server" Label="Role" Help="The group role to use when adding new members to the group. If the group does not have this role then the default role will be used instead." Visible="false" Required="true" />
                                            <Rock:RockDropDownList ID="ddlSettingsAddAsStatus" runat="server" Label="Add As Status" Help="The member status to add new members to the group with. Group and Role capacities will only be enforced if this is set to Active." Required="true" />
                                            <Rock:RockDropDownList ID="ddlSettingsRequestMemberAttributes" runat="server" Label="Request Member Attributes" Help="If a Group has Member Attributes defined then you can set if you want those attributes to be filled in by the user. If set to Required then the user will only be prompted for Attributes that are marked as Required. If set to All then the user will be prompted for all Attributes. Otherwise no attributes will be prompted for." Required="true">
                                                <asp:ListItem Text="None" Value="None" />
                                                <asp:ListItem Text="Required" Value="Required" />
                                                <asp:ListItem Text="All" Value="All" />
                                            </Rock:RockDropDownList>
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-6">
                                    <div class="panel panel-default">
                                        <div class="panel-heading">
                                            <h3 class="panel-title">Limitations</h3>
                                        </div>
                                        <div class="panel-body">
                                            <Rock:RockCheckBox ID="cbSettingsAllowRemove" runat="server" Label="Allow Remove" Help="Allow a user to remove themselves from a group they are already in. If No then any checkboxes will be disabled for these groups. Radio buttons can still be deselected, however the internal logic will not remove them from the group." />
                                            <Rock:NumberBox ID="nbSettingsMinimumSelection" runat="server" Label="Minimum Selection" Help="The minimum number of selections that the user must select before proceeding." NumberType="Integer" MinimumValue="0" MaximumValue="2147483647" Required="true" />
                                            <Rock:NumberBox ID="nbSettingsMaximumSelection" runat="server" Label="Maximum Selection" Help="The maximum number of checkboxes that the user can select. Has no effect on radio buttons. Set to 0 for unlimited." NumberType="Integer" MinimumValue="0" MaximumValue="2147483647" Required="true" />
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="row">
                                <div class="col-md-12">
                                    <div class="panel panel-default">
                                        <div class="panel-heading">
                                            <h3 class="panel-title">Post-Save Actions</h3>
                                        </div>
                                        <div class="panel-body">
                                            <div class="row">
                                                <div class="col-md-6">
                                                    <Rock:PagePicker ID="ppSettingsSaveRedirectPage" runat="server" Label="Save Redirect Page" Help="The page to redirect the user to after all their changes have been saved." Required="false" />
                                                    <Rock:WorkflowTypePicker ID="wtpSettingsIndividualWorkflow" runat="server" Label="Individual Workflow" Help="Activate the selected workflow for each individual GroupMember record created (also fires if an GroupMember changes from Inactive to Pending or Active). The GroupMember is passed as the Entity to the workflow." Required="false" />
                                                </div>
                                                <div class="col-md-6">
                                                    <Rock:WorkflowTypePicker ID="wtpSettingsSubmissionWorkflow" runat="server" Label="Submission Workflow" Help="Activate the selected workflow one time for each submission. The CurrentPerson is passed as the Entity to the workflow." Required="false" OnSelectItem="wtpSettingsSubmissionWorkflow_SelectItem" />
                                                    <Rock:RockDropDownList ID="ddlSettingsSubmissionAttribute" runat="server" Label="Submission Attribute" Help="Attribute to store the group member GUIDs into as a comma separated list." Required="false" />
                                                </div>
                                            </div>
                                            <div class="row">
                                                <div class="col-md-12">
                                                    <Rock:CodeEditor ID="ceSettingsSavedTemplate" runat="server" Label="Saved Template" Help="Message to be displayed to the user once all their selections have been saved. Lava objects 'Added' and 'Removed' are arrays of GroupMember objects for the groups they were added or removed from." Required="false" EditorMode="Lava" EditorHeight="200" EditorTheme="Rock" />
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="row">
                                <div class="col-md-12">
                                    <div class="panel panel-default">
                                        <div class="panel-heading">
                                            <h3 class="panel-title">User Interface</h3>
                                        </div>
                                        <div class="panel-body">
                                            <Rock:RockTextBox ID="tbSettingsSubmitTitle" runat="server" Label="Submit Title" Help="Title of the Submit button to show to the user." Required="true" />
                                            <Rock:CodeEditor ID="ceSettingsContentTemplate" runat="server" Label="Content Template" Help="Template to use for the content that generates the checkboxes or radio buttons. Any checkbox or radio button will automatically be selected and enabled/disabled as needed. The Lava property Name can be used as a unique name key for the input controls though it is not required to match. If a checkbox or radio button is disabled or enabled then a jQuery disabled and enabled event will be triggered allowing you to do custom UI updates." Required="true" EditorMode="Lava" EditorHeight="400" EditorTheme="Rock" />
                                            <Rock:RockCheckBox ID="cbSettingsLavaDebug" runat="server" Label="Lava Debug" Help="Show the Lava Debug panel which contains detailed information about what fields are available in the Content Template." />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </Content>
            </Rock:ModalDialog>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript">
    (function ($) {
        function setup()
        {
            new SelfJoin('<%= pnlContent.ClientID %>', '<%= hfSelection.ClientID %>', '<%= btnSubmit.ClientID %>', <%= MinimumSelection %>, <%= MaximumSelection %>, '<%= LockedValues %>');
        }

        //
        // Triggered on a clean document load. Delay to let other jQuery run first.
        //
        $(document).ready(function () {
            setTimeout(function() { setup(); }, 10);
        });

        //
        // Triggered on an UpdatePanel postback. Delay to let other jQuery run first.
        //
        var prm = Sys.WebForms.PageRequestManager.getInstance();
        prm.add_endRequest(function () {
            setTimeout(function () { setup(); }, 10);
        });
    })(jQuery);
</script>
