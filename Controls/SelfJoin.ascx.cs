using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Extension;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.SelfJoin
{
    [DisplayName( "Self Join" )]
    [Category( "com_shepherdchurch > Self Join" )]
    [Description( "Allows logged in users to join and un-join from specific groups." )]

    [GroupField( "Group", "The group to use as a root for showing selections.", true, category: "CustomSetting" )]
    [GroupRoleField("", "Group Role", "The group role to use when adding new members to the group. If the group does not have this role then the group's default role will be used.", true, category: "CustomSetting" )]
    [EnumField( "Add As Status", "The member status to add new members to the group with.", typeof( GroupMemberStatus ), true, "2", category: "CustomSetting" )]
    [CustomDropdownListField( "Request Member Attributes", "Request Group Member attributes be filled in by the user. If set to Required then only Attributes that are marked as Required will be prompted for. If set to All then all Attributes will be prompted for. Otherwise no attributes will be prompted for.", "None,Required,All", true, "None", category: "CustomSetting" )]

    [BooleanField( "Allow Remove", "Allow a user to remove themselves from a group they are already in.", false, category: "CustomSetting" )]
    [IntegerField( "Minimum Selection", "The minimum number of selections that the user must select before proceeding.", true, 0, category: "CustomSetting" )]
    [IntegerField( "Maximum Selection", "The maximum number of checkboxes that the user can select. Has no effect on radio buttons. Set to 0 for unlimited.", true, 0, category: "CustomSetting" )]

    [TextField( "Submit Title", "Title of the Submit button to show to the user.", true, "Submit", category: "CustomSetting" )]
    [CodeEditorField( "Content Template", "Template to use for the content that generates the checkboxes or radio buttons. Any checkbox or radio button will automatically be selected and enabled/disabled as needed. The Lava property Name can be used as a unique name key for the input controls though it is not required to match.", Rock.Web.UI.Controls.CodeEditorMode.Lava, height: 400, required: true, category: "CustomSetting", defaultValue: @"<ul class=""rocktree"">
    {% for g1 in Group.Groups %}
    <li>
        <div class=""checkbox"">
            <label><input type=""checkbox"" value=""{{ g1.Guid }}""> {{ g1.Name }}</label>
        </div>
        {% if g1.Groups != empty %}
            <ul class=""rocklist"">
                {% for g2 in g1.Groups %}
                <li>
                    <div class=""checkbox"">
                        <label><input type=""checkbox"" value=""{{ g2.Guid }}""> {{ g2.Name }}</label>
                    </div>
                    {% if g1.Groups != empty %}
                        <ul class=""rocklist"">
                            {% for g3 in g2.Groups %}
                            <li>
                                <div class=""checkbox"">
                                    <label><input type=""checkbox"" value=""{{ g3.Guid }}""> {{ g3.Name }}</label>
                                </div>
                            </li>
                            {% endfor %}
                        </ul>
                    {% endif %}
                </li>
                {% endfor %}
            </ul>
        {% endif %}
    </li>
    {% endfor %}
</ul>" )]
    [BooleanField( "Lava Debug", "Show the Lava Debug panel which contains detailed information about what fields are available in the Content Template.", category: "CustomSetting" )]
    public partial class SelfJoin : RockBlockCustomSettings
    {
        Group _group = null;
        protected int MinimumSelection = 0;
        protected int MaximumSelection = 0;
        protected string LockedValues = string.Empty;
        bool IsGroupMembershipRebind = false;

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            RockPage.AddScriptLink( "~/Plugins/com_shepherdchurch/SelfJoin/Scripts/SelfJoin.js" );
            btnSubmit.Text = GetAttributeValue( "SubmitTitle" );
            btnAttributesSubmit.Text = btnSubmit.Text;

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "Group" ) ) )
            {
                _group = new GroupService( new RockContext() ).Get( GetAttributeValue( "Group" ).AsGuid() );
            }

            if ( !IsPostBack )
            {
                ShowGroups( true );
            }

            if ( pnlGroupAttributes.Visible )
            {
                BindGroupMembership( false, GetAttributeMembershipRecords() );
            }
        }

        /// <summary>
        /// Get a list of group GUIDs that the current person is a member of at or below the
        /// primary Group.
        /// </summary>
        /// <param name="group">Current group in the recursive call to check.</param>
        /// <returns>Array of string GUIDs for which groups the user is a member of.</returns>
        string[] GetExistingMembership(Group group)
        {
            List<string> membership = new List<string>();

            if ( group != null )
            {
                if ( group.Members.Where( m => m.PersonId == CurrentPerson.Id ).Count() != 0 )
                {
                    membership.Add( group.Guid.ToString() );
                }

                foreach ( Group g in group.Groups )
                {
                    membership.AddRange( GetExistingMembership( g ) );
                }
            }

            return membership.ToArray();
        }

        /// <summary>
        /// Generate the content from the user Lava.
        /// </summary>
        void ShowGroups( bool setValues )
        {
            string template = GetAttributeValue( "ContentTemplate" ) ?? string.Empty;
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
            string[] membership = GetExistingMembership( _group );

            //
            // Set some variables that the .ASCX file will use to initialize the Javascript.
            //
            MinimumSelection = GetAttributeValue( "MinimumSelection" ).AsInteger();
            MaximumSelection = GetAttributeValue( "MaximumSelection" ).AsInteger();
            LockedValues = (GetAttributeValue( "AllowRemove" ).AsBoolean( false ) ? string.Empty : string.Join( ",", membership ));
            if ( setValues )
            {
                hfSelection.Value = string.Join( ",", membership );
            }

            //
            // Add our custom merge fields and generate the content.
            //
            mergeFields.Add( "Group", _group );
            mergeFields.Add( "Name", ClientID );
            mergeFields.Add( "Membership", membership );
            ltContent.Text = template.ResolveMergeFields( mergeFields );

            //
            // If they have Lava Debug turned on then dump out the lava debug information.
            //
            if ( GetAttributeValue( "LavaDebug" ).AsBoolean( false ) == true && pnlGroupList.Visible )
            {
                pnlLavaDebug.Visible = true;
                ltLavaDebug.Text = mergeFields.lavaDebugInfo();
            }
            else
            {
                pnlLavaDebug.Visible = false;
            }
        }

        /// <summary>
        /// User has clicked on the "Settings" button in the admin panel.
        /// </summary>
        protected override void ShowSettings()
        {
            Group group = new GroupService( new RockContext() ).Get( GetAttributeValue( "Group" ).AsGuid() );

            if ( group != null && group.Id != 0 )
            {
                gpSettingsGroup.SetValue( group );
                grpSettingsRole.GroupTypeId = group.GroupTypeId;
                grpSettingsRole.GroupRoleId = GetAttributeValue( "GroupRole" ).AsInteger();
                grpSettingsRole.Visible = true;
            }
            else
            {
                gpSettingsGroup.SetValue( null );
                grpSettingsRole.Visible = false;
            }

            ddlSettingsAddAsStatus.BindToEnum<GroupMemberStatus>( true, new GroupMemberStatus[] { GroupMemberStatus.Inactive } );
            ddlSettingsAddAsStatus.SelectedValue = GetAttributeValue( "AddAsStatus" );

            ddlSettingsRequestMemberAttributes.Items.Clear();
            ddlSettingsRequestMemberAttributes.Items.Add( "None" );
            ddlSettingsRequestMemberAttributes.Items.Add( "Required" );
            ddlSettingsRequestMemberAttributes.Items.Add( "All" );
            ddlSettingsRequestMemberAttributes.SelectedValue = GetAttributeValue( "RequestMemberAttributes" );

            cbSettingsAllowRemove.Checked = GetAttributeValue( "AllowRemove" ).AsBoolean();
            nbSettingsMinimumSelection.Text = GetAttributeValue( "MinimumSelection" );
            nbSettingsMaximumSelection.Text = GetAttributeValue( "MaximumSelection" );

            tbSettingsSubmitTitle.Text = GetAttributeValue( "SubmitTitle" );
            ceSettingsContentTemplate.Text = GetAttributeValue( "ContentTemplate" );
            cbSettingsLavaDebug.Checked = GetAttributeValue( "LavaDebug" ).AsBoolean();

            pnlSettingsModal.Visible = true;
            mdSettings.Show();
        }

        /// <summary>
        /// Get a list of GroupMember records for the user's selection. This includes all new
        /// GroupMember records that would need to be saved into the database as well as any
        /// existing membership records.
        /// </summary>
        /// <returns>List of GroupMember objects for the current person.</returns>
        List<GroupMember> GetMembershipRecords()
        {
            RockContext rockContext = new RockContext();
            List<GroupMember> groupsMembership = new List<GroupMember>();
            List<GroupMember> attributeMembers = new List<GroupMember>();
            GroupMemberService memberService = new GroupMemberService( rockContext );
            GroupService groupService = new GroupService( rockContext );
            GroupMemberStatus status = ( GroupMemberStatus )Enum.Parse( typeof( GroupMemberStatus ), (GetAttributeValue( "AddAsStatus" ) ?? string.Empty) );

            foreach ( string g in hfSelection.Value.Split( ',' ) )
            {
                GroupMember member = null;
                Group group = null;
                Guid groupGuid = g.AsGuid();

                //
                // Could not parse the GUID or find the Group, skip it.
                //
                if ( groupGuid == Guid.Empty || (group = groupService.Get( groupGuid )).Id == 0 )
                {
                    continue;
                }

                //
                // Find the existing record, otherwise create a new one.
                //
                member = group.Members.Where( gm => gm.PersonId == CurrentPersonId ).FirstOrDefault();
                if ( member == null )
                {
                    member = new GroupMember { Id = 0 };
                    member.Group = group;
                    member.GroupId = member.Group.Id;
                    member.Person = CurrentPerson;
                    member.PersonId = member.Person.Id;
                    member.DateTimeAdded = RockDateTime.Now;
                    member.GroupRoleId = member.Group.GroupType.DefaultGroupRoleId ?? 0;
                    member.GroupMemberStatus = status;
                }
                else if ( member.GroupMemberStatus == GroupMemberStatus.Inactive )
                {
                    //
                    // We only update the status if it is Inactive.
                    //
                    member.GroupMemberStatus = status;
                }

                member.LoadAttributes( rockContext );
                groupsMembership.Add( member );
            }

            return groupsMembership;
        }

        /// <summary>
        /// Retrieve a list of GroupMember records that need Attributes supplied by the user.
        /// </summary>
        /// <returns>A List collection of GroupMembers.</returns>
        List<GroupMember> GetAttributeMembershipRecords()
        {
            string requestMemberAttributes = GetAttributeValue( "RequestMemberAttributes" );

            if ( requestMemberAttributes == "All" )
            {
                return GetMembershipRecords().Where( gm => gm.Attributes.Any() ).OrderBy( gm => gm.GroupId ).ToList();
            }
            else if ( requestMemberAttributes == "Required" )
            {
                return GetMembershipRecords().Where( gm => gm.Attributes.Values.Where( a => a.IsRequired ).Any() ).OrderBy( gm => gm.GroupId ).ToList();
            }
            else
            {
                return new List<GroupMember>();
            }
        }

        /// <summary>
        /// Bind the Group Attributes repeater to the list of GroupMembers.
        /// </summary>
        /// <param name="setValues">Set the values or use the existing values from the web UI.</param>
        /// <param name="members">List of GroupMember records to bind to.</param>
        void BindGroupMembership( bool setValues, List<GroupMember> members )
        {
            IsGroupMembershipRebind = !setValues;
            rptrGroupAttributes.DataSource = members;
            rptrGroupAttributes.DataBind();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptrGroupAttributes control.
        /// </summary>
        /// <param name="sender">The object that sent the message.</param>
        /// <param name="e">Event arguments.</param>
        protected void rptrGroupAttributes_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            PlaceHolder phAttributes = e.Item.FindControl( "phAttributes" ) as PlaceHolder;

            Helper.AddEditControls( ( GroupMember )e.Item.DataItem, phAttributes, !IsGroupMembershipRebind, BlockValidationGroup, true );
        }

        /// <summary>
        /// Handles the Click event of the btnSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSubmit_Click( object sender, EventArgs e )
        {
            pnlGroupList.Visible = false;
            pnlLavaDebug.Visible = false;
            pnlGroupAttributes.Visible = true;

            rptrGroupAttributes.Controls.Clear();
            BindGroupMembership( true, GetAttributeMembershipRecords() );
        }

        /// <summary>
        /// Handles the Click event of the btnAttributesBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAttributesBack_Click( object sender, EventArgs e )
        {
            pnlGroupList.Visible = true;
            pnlGroupAttributes.Visible = false;

            ShowGroups( false );
        }

        /// <summary>
        /// Handles the Click event of the btnAttributesSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAttributesSubmit_Click( object sender, EventArgs e )
        {
            var members = GetAttributeMembershipRecords();

            if (members.Count == rptrGroupAttributes.Controls.Count)
            {
                //
                // This is a pretty serious hack, but since a placeholder is a really dumb control it should
                // be safe to do this. We are accessing the contents of the PlaceHolder directly to find the
                // current values of the attributes.
                //
                for ( int i = 0; i < members.Count; i++ )
                {
                    GroupMember member = members[i];
                    PlaceHolder phAttributes = ( PlaceHolder )rptrGroupAttributes.Controls[i].FindControl( "phAttributes" );

                    Helper.GetEditValues( phAttributes, member );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSettingsSave_Click( object sender, EventArgs e )
        {
            Group group = new GroupService( new RockContext() ).Get( gpSettingsGroup.SelectedValue.AsInteger() );

            SetAttributeValue( "Group", (group.Id != 0 ? group.Guid.ToString() : string.Empty) );
            SetAttributeValue( "GroupRole", (grpSettingsRole.GroupRoleId.HasValue ? grpSettingsRole.GroupRoleId.Value.ToString() : string.Empty) );
            SetAttributeValue( "AddAsStatus", ddlSettingsAddAsStatus.SelectedValue );
            SetAttributeValue( "RequestMemberAttributes", ddlSettingsRequestMemberAttributes.SelectedValue );

            SetAttributeValue( "AllowRemove", cbSettingsAllowRemove.Checked.ToString() );
            SetAttributeValue( "MinimumSelection", nbSettingsMinimumSelection.Text );
            SetAttributeValue( "MaximumSelection", nbSettingsMaximumSelection.Text );

            SetAttributeValue( "SubmitTitle", tbSettingsSubmitTitle.Text );
            SetAttributeValue( "ContentTemplate", ceSettingsContentTemplate.Text );
            SetAttributeValue( "LavaDebug", cbSettingsLavaDebug.Checked.ToString() );

            SaveAttributeValues();

            mdSettings.Hide();
            pnlSettingsModal.Visible = false;

            ShowGroups( true );
        }

        /// <summary>
        /// User has changed selection on the Group Picker. Update the Role picker to match.
        /// </summary>
        /// <param name="sender">Object that has initiated this event.</param>
        /// <param name="e">Event arguments that describe this event.</param>
        protected void gpSettingsGroup_SelectItem( object sender, EventArgs e )
        {
            int? groupId = gpSettingsGroup.SelectedValueAsId();
            Group group = null;

            if ( groupId.HasValue )
            {
                group = new GroupService( new RockContext() ).Get( groupId.Value );
            }

            if ( group != null && group.Id != 0 )
            {
                grpSettingsRole.GroupTypeId = group.GroupTypeId;
                grpSettingsRole.Visible = true;
            }
            else
            {
                grpSettingsRole.Visible = false;
            }
        }
    }
}
