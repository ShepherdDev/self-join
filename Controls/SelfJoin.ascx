﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SelfJoin.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.SelfJoin.SelfJoin" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Warning" />

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
                                            <Rock:GroupPicker ID="gpSettingsGroup" runat="server" Label="Group" Help="The group to use as a root for showing selections." Required="true" OnSelectItem="gpSettingsGroup_SelectItem" />
                                            <Rock:GroupRolePicker ID="grpSettingsRole" runat="server" Label="Role" Help="The group role to use when adding new members to the group." Visible="false" Required="true" />
                                            <Rock:RockDropDownList ID="ddlSettingsAddAsStatus" runat="server" Label="Add As Status" Help=" If the group does not have this role then the group's default role will be used." Required="true" />
                                            <Rock:RockDropDownList ID="ddlSettingsRequestMemberAttributes" runat="server" Label="Request Member Attributes" Help="Request Group Member attributes be filled in by the user. If set to Required then only Attributes that are marked as Required will be prompted for. If set to All then all Attributes will be prompted for. Otherwise no attributes will be prompted for." Required="true" />
                                        </div>
                                    </div>
                                </div>

                                <div class="col-md-6">
                                    <div class="panel panel-default">
                                        <div class="panel-heading">
                                            <h3 class="panel-title">Limitations</h3>
                                        </div>
                                        <div class="panel-body">
                                            <Rock:RockCheckBox ID="cbSettingsAllowRemove" runat="server" Label="Allow Remove" Help="Allow a user to remove themselves from a group they are already in." />
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
                                            <h3 class="panel-title">User Interface</h3>
                                        </div>
                                        <div class="panel-body">
                                            <Rock:RockTextBox ID="tbSettingsSubmitTitle" runat="server" Label="Submit Title" Help="Title of the Submit button to show to the user." Required="true" />
                                            <Rock:CodeEditor ID="ceSettingsContentTemplate" runat="server" Label="Content Template" Help="Template to use for the content that generates the checkboxes or radio buttons. Any checkbox or radio button will automatically be selected and enabled/disabled as needed. The Lava property Name can be used as a unique name key for the input controls though it is not required to match." Required="true" EditorMode="Lava" EditorHeight="400" EditorTheme="Rock" />
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
