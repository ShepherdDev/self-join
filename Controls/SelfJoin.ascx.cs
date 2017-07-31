using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.SelfJoin
{
    [DisplayName( "Self Join" )]
    [Category( "Shepherd Church > Self Join" )]
    [Description( "Allows logged in users to join and un-join from specific groups." )]

    #region Block Fields

    [GroupField( "Group", "The group to use as a root for showing selections. This is passed to the Lava parser as a property called Group and can be used to build the list of checkboxes or radio buttons.", false, category: "CustomSetting" )]
    [GroupRoleField("", "Group Role", "The group role to use when adding new members to the group. If the group does not have this role then the default role will be used instead.", false, category: "CustomSetting" )]
    [EnumField( "Add As Status", "The member status to add new members to the group with. Group and Role capacities will only be enforced if this is set to Active.", typeof( GroupMemberStatus ), true, "2", category: "CustomSetting" )]
    [CustomDropdownListField( "Request Member Attributes", "If a Group has Member Attributes defined then you can set if you want those attributes to be filled in by the user. If set to Required then the user will only be prompted for Attributes that are marked as Required. If set to All then the user will be prompted for all Attributes. Otherwise no attributes will be prompted for.", "None,Required,All", true, "None", category: "CustomSetting" )]
    [LinkedPage( "Cancel Page", "If a Cancel button should be displayed then select the page the user will be redirected to.", false, category: "CustomSetting" )]

    [BooleanField( "Allow Remove", "Allow a user to remove themselves from a group they are already in. If No then any checkboxes will be disabled for these groups. Radio buttons can still be deselected, however the internal logic will not remove them from the group.", false, category: "CustomSetting" )]
    [IntegerField( "Minimum Selection", "The minimum number of selections that the user must select before proceeding.", true, 0, category: "CustomSetting" )]
    [IntegerField( "Maximum Selection", "The maximum number of checkboxes that the user can select. Has no effect on radio buttons. Set to 0 for unlimited.", true, 0, category: "CustomSetting" )]

    [LinkedPage( "Save Redirect Page", "The page to redirect the user to after all their changes have been saved.", false, category: "CustomSetting" )]
    [WorkflowTypeField( "Individual Workflow", "Activate the selected workflow for each individual GroupMember record created (also fires if an GroupMember changes from Inactive to Pending or Active). The GroupMember is passed as the Entity to the workflow.", category: "CustomSetting" )]
    [WorkflowTypeField( "Submission Workflow", "Activate the selected workflow one time for each submission. The CurrentPerson is passed as the Entity to the workflow.", category: "CustomSetting" )]
    [TextField( "Submission Attribute", "Attribute to store the group member GUIDs into as a comma separated list.", false, "", category: "CustomSetting" )]
    [CodeEditorField( "Saved Template", "Message to be displayed to the user once all their selections have been saved. Lava objects 'Added' and 'Removed' are arrays of GroupMember objects for the groups they were added or removed from.", Rock.Web.UI.Controls.CodeEditorMode.Lava, height: 400, category: "CustomSetting", defaultValue: @"Thank you for your interest. You have been added to the following groups:
<ul>
    {% for gm in Added %}
    <li>{{ gm.Group.Name }}</li>
    {% endfor %}
</ul>" )]

    [TextField( "Submit Title", "Title of the Submit button to show to the user.", true, "Submit", category: "CustomSetting" )]
    [TextField( "Header Title", "The title to use for the block.", true, "Options", category: "CustomSetting" )]
    [BooleanField( "Kiosk Mode", "Changes interface to support kiosk mode with touch-based scrolling.", false, category: "CustomSetting" )]
    [CodeEditorField( "Content Template", "Template to use for the content that generates the checkboxes or radio buttons. Any checkbox or radio button will automatically be selected and enabled/disabled as needed. The Lava property Name can be used as a unique name key for the input controls though it is not required to match. If a checkbox or radio button is disabled or enabled then a jQuery disabled and enabled event will be triggered allowing you to do custom UI updates.", Rock.Web.UI.Controls.CodeEditorMode.Lava, height: 400, required: true, category: "CustomSetting", defaultValue: @"<ul class=""rocktree"">
    {% for g1 in Group.Groups %}
    {% if g1.IsActive == true and g1.IsPublic == true %}
    <li>
        <div class=""checkbox"">
            <label><input type=""checkbox"" value=""{{ g1.Guid }}""> {{ g1.Name }}</label>
        </div>
        {% if g1.Groups != empty %}
            <ul class=""rocklist"">
                {% for g2 in g1.Groups %}
                {% if g2.IsActive == true and g2.IsPublic == true %}
                <li>
                    <div class=""checkbox"">
                        <label><input type=""checkbox"" value=""{{ g2.Guid }}""> {{ g2.Name }}</label>
                    </div>
                    {% if g1.Groups != empty %}
                        <ul class=""rocklist"">
                            {% for g3 in g2.Groups %}
                            {% if g3.IsActive == true and g3.IsPublic == true %}
                            <li>
                                <div class=""checkbox"">
                                    <label><input type=""checkbox"" value=""{{ g3.Guid }}""> {{ g3.Name }}</label>
                                </div>
                            </li>
                            {% endif %}
                            {% endfor %}
                        </ul>
                    {% endif %}
                </li>
                {% endif %}
                {% endfor %}
            </ul>
        {% endif %}
    </li>
    {% endif %}
    {% endfor %}
</ul>" )]
    [BooleanField( "Lava Debug", "Show the Lava Debug panel which contains detailed information about what fields are available in the Content Template.", category: "CustomSetting" )]

    #endregion

    public partial class SelfJoin : RockBlockCustomSettings
    {
        #region Properties and Fields

        Group _group = null;
        bool _isGroupMembershipRebind = false;
        bool _isKoiskMode = false;
        Person _person = null;

        protected int MinimumSelection = 0;
        protected int MaximumSelection = 0;
        protected string LockedValues = string.Empty;

        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            _isKoiskMode = GetAttributeValue( "KioskMode" ).AsBoolean( false );
            if ( _isKoiskMode )
            {
                RockPage.AddScriptLink( "~/Scripts/iscroll.js" );
                RockPage.AddScriptLink( "~/Scripts/Kiosk/kiosk-core.js" );
            }

            RockPage.AddScriptLink( "~/Plugins/com_shepherdchurch/SelfJoin/Scripts/SelfJoin.js" );

            _person = CurrentPerson;
            if ( !string.IsNullOrWhiteSpace( PageParameter( "Person" ) ) )
            {
                _person = new PersonService( new RockContext() ).Get( PageParameter( "Person" ).AsGuid() );
            }
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            btnSubmit.Text = GetAttributeValue( "SubmitTitle" );
            btnSubmitKiosk.Text = GetAttributeValue( "SubmitTitle" );
            btnAttributesSubmit.Text = btnSubmit.Text;
            btnCancel.Visible = !string.IsNullOrWhiteSpace( GetAttributeValue( "CancelPage" ) );
            btnCancelKiosk.Visible = btnCancel.Visible;

            if ( _person == null )
            {
                nbWarningMessage.Text = "You must be logged in to properly view this page.";
                return;
            }

            if ( !string.IsNullOrWhiteSpace( PageParameter( "Group" ) ) )
            {
                _group = new GroupService( new RockContext() ).Get( PageParameter( "Group" ).AsGuid() );
            }
            else if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "Group" ) ) )
            {
                _group = new GroupService( new RockContext() ).Get( GetAttributeValue( "Group" ).AsGuid() );
            }

            if ( !IsPostBack )
            {
                pnlGroupList.Visible = !_isKoiskMode;
                pnlGroupListKiosk.Visible = _isKoiskMode;

                ShowGroups( true );
            }

            if ( pnlGroupAttributes.Visible || pnlGroupAttributesKiosk.Visible )
            {
                BindGroupMembership( false, GetAttributeMembershipRecords() );
            }
        }

        /// <summary>
        /// User has clicked on the "Settings" button in the admin panel.
        /// </summary>
        protected override void ShowSettings()
        {
            Group group = new GroupService( new RockContext() ).Get( GetAttributeValue( "Group" ).AsGuid() );

            //
            // Fill in the General section.
            //
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
            ddlSettingsRequestMemberAttributes.SelectedValue = GetAttributeValue( "RequestMemberAttributes" );
            ppCancelPage.SetValue( new PageService( new RockContext() ).Get( GetAttributeValue( "CancelPage" ).AsGuid() ) );

            //
            // Fill in the Limitation section.
            //
            cbSettingsAllowRemove.Checked = GetAttributeValue( "AllowRemove" ).AsBoolean();
            nbSettingsMinimumSelection.Text = GetAttributeValue( "MinimumSelection" );
            nbSettingsMaximumSelection.Text = GetAttributeValue( "MaximumSelection" );

            //
            // Fill in the Post-Save Actions section.
            //
            wtpSettingsIndividualWorkflow.SetValue( (!string.IsNullOrWhiteSpace( GetAttributeValue( "IndividualWorkflow" ) ) ? new WorkflowTypeService( new RockContext() ).Get( GetAttributeValue( "IndividualWorkflow" ).AsGuid() ) : null) );
            var submissionWorkflowType = new WorkflowTypeService( new RockContext() ).Get( GetAttributeValue( "SubmissionWorkflow" ).AsGuid() );
            wtpSettingsSubmissionWorkflow.SetValue( (submissionWorkflowType != null ? submissionWorkflowType : null) );
            string selection = GetAttributeValue( "SubmissionAttribute" );
            LoadSubmissionWorkflowAttributes( submissionWorkflowType );
            ddlSettingsSubmissionAttribute.SelectedValue = ddlSettingsSubmissionAttribute.Items.FindByValue( selection ) != null ? selection : string.Empty;
            ppSettingsSaveRedirectPage.SetValue( (!string.IsNullOrWhiteSpace( GetAttributeValue( "SaveRedirectPage" ) ) ? new PageService( new RockContext() ).Get( GetAttributeValue( "SaveRedirectPage" ).AsGuid() ) : null) );
            ceSettingsSavedTemplate.Text = GetAttributeValue( "SavedTemplate" );

            //
            // Fill in the User Interface section.
            //
            tbSettingsSubmitTitle.Text = GetAttributeValue( "SubmitTitle" );
            tbSettingsHeaderTitle.Text = GetAttributeValue( "HeaderTitle" );
            cbSettingsKioskMode.Checked = GetAttributeValue( "KioskMode" ).AsBoolean( false );
            ceSettingsContentTemplate.Text = GetAttributeValue( "ContentTemplate" );
            cbSettingsLavaDebug.Checked = GetAttributeValue( "LavaDebug" ).AsBoolean();

            pnlSettingsModal.Visible = true;
            mdSettings.Show();
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Get a list of group GUIDs that the current person is a member of at or below the
        /// primary Group.
        /// </summary>
        /// <param name="group">Current group in the recursive call to check.</param>
        /// <returns>Array of string GUIDs for which groups the user is a member of.</returns>
        List<GroupMember> GetExistingMembership(Group group)
        {
            List<GroupMember> membership = new List<GroupMember>();

            if ( group != null )
            {
                GroupMember member = group.Members.Where( m => m.PersonId == _person.Id && m.GroupMemberStatus != GroupMemberStatus.Inactive ).FirstOrDefault();

                if ( group.IsActive && member != null && member.Id != 0 )
                {
                    membership.Add( member );
                }

                foreach ( Group g in group.Groups )
                {
                    membership.AddRange( GetExistingMembership( g ) );
                }
            }

            return membership;
        }

        /// <summary>
        /// Generate the content from the user Lava.
        /// </summary>
        void ShowGroups( bool setValues )
        {
            string template = GetAttributeValue( "ContentTemplate" ) ?? string.Empty;
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, _person );
            List<GroupMember> membership = GetExistingMembership( _group );
            string membershipString = membership.Select( m => m.Group.Guid.ToString() ).ToList().AsDelimited( "," );

            //
            // Set some variables that the .ASCX file will use to initialize the Javascript.
            //
            MinimumSelection = GetAttributeValue( "MinimumSelection" ).AsInteger();
            MaximumSelection = GetAttributeValue( "MaximumSelection" ).AsInteger();
            LockedValues = (GetAttributeValue( "AllowRemove" ).AsBoolean( false ) ? string.Empty : membershipString);
            if ( setValues )
            {
                hfSelection.Value = membershipString;
            }

            //
            // Add our custom merge fields and generate the content.
            //
            mergeFields.Add( "Group", _group );
            mergeFields.Add( "Name", ClientID );
            mergeFields.Add( "Membership", membership.Select( m => m.Group.Guid.ToString() ).ToList() );
            if ( !_isKoiskMode )
            {
                ltContent.Text = template.ResolveMergeFields( mergeFields );
            }
            else
            {
                ltContentKiosk.Text = template.ResolveMergeFields( mergeFields );
            }

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
        /// Get a list of GroupMember records for the user's selection. This includes all new
        /// GroupMember records that would need to be saved into the database as well as any
        /// existing membership records.
        /// </summary>
        /// <returns>List of GroupMember objects for the current person.</returns>
        List<GroupMember> GetMembershipRecords( RockContext rockContext = null )
        {
            if ( rockContext == null )
            {
                rockContext = new RockContext();
            }

            List<GroupMember> groupsMembership = new List<GroupMember>();
            List<GroupMember> attributeMembers = new List<GroupMember>();
            GroupMemberService memberService = new GroupMemberService( rockContext );
            GroupService groupService = new GroupService( rockContext );
            Person person = new PersonService( rockContext ).Get( _person.Id );
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
                // Do not allow people to add to security groups.
                //
                if ( group.IsSecurityRole && !string.IsNullOrWhiteSpace( PageParameter( "Group" ) ) )
                {
                    continue;
                }

                //
                // Find the existing record, otherwise create a new one.
                //
                member = group.Members.Where( gm => gm.PersonId == _person.Id ).FirstOrDefault();
                if ( member == null )
                {
                    member = new GroupMember { Id = 0 };
                    rockContext.GroupMembers.Add( member );

                    member.Group = group;
                    member.GroupId = member.Group.Id;
                    member.Person = person;
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
        List<GroupMember> GetAttributeMembershipRecords( List<GroupMember> members = null, RockContext rockContext = null )
        {
            string requestMemberAttributes = GetAttributeValue( "RequestMemberAttributes" );

            if ( members == null )
            {
                members = GetMembershipRecords( rockContext );
            }

            if ( requestMemberAttributes == "All" )
            {
                return members.Where( gm => gm.Attributes.Any() ).OrderBy( gm => gm.GroupId ).ToList();
            }
            else if ( requestMemberAttributes == "Required" )
            {
                return members.Where( gm => gm.Attributes.Values.Where( a => a.IsRequired ).Any() ).OrderBy( gm => gm.GroupId ).ToList();
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
            Repeater rptr = pnlGroupAttributes.Visible ? rptrGroupAttributes : rptrGroupAttributesKiosk;

            _isGroupMembershipRebind = !setValues;
            rptr.DataSource = members;
            rptr.DataBind();
        }

        /// <summary>
        /// Remove (make Inactive) the user from any groups that they have unselected.
        /// </summary>
        /// <param name="rockContext">Data context to make changes in.</param>
        /// <param name="membership">The membership records that should remain.</param>
        void RemoveFromUnselectedGroups( RockContext rockContext, List<GroupMember> membership )
        {
            if ( GetAttributeValue( "AllowRemove" ).AsBoolean() == true )
            {
                List<GroupMember> originalMembership = GetExistingMembership( new GroupService( rockContext ).Get( _group.Id ) );

                originalMembership = originalMembership.Where( om => !membership.Select( m => m.Id ).ToList().Contains( om.Id ) ).ToList();
                originalMembership.ForEach( om => { om.GroupMemberStatus = GroupMemberStatus.Inactive; } );
            }
        }

        /// <summary>
        /// Perform the final SaveChanges operation on the context and trigger any on-save events
        /// like workflows and "thank you" message or page redirect.
        /// </summary>
        /// <param name="rockContext">The Database Context that contains all the changes.</param>
        protected void Save( RockContext rockContext )
        {
            var added = rockContext.ChangeTracker.Entries<GroupMember>().Where( c => c.State == EntityState.Added || (c.State == EntityState.Modified && ( GroupMemberStatus )c.OriginalValues["GroupMemberStatus"] == GroupMemberStatus.Inactive) ).Select( c => c.Entity ).ToList();
            var removed = rockContext.ChangeTracker.Entries<GroupMember>().Where( c => c.State == EntityState.Modified && ( GroupMemberStatus )c.CurrentValues["GroupMemberStatus"] == GroupMemberStatus.Inactive ).Select( c => c.Entity ).ToList();

            rockContext.SaveChanges();
            TriggerWorkflows( added );

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "SavedTemplate" ) ) )
            {
                string template = GetAttributeValue( "SavedTemplate" ) ?? string.Empty;
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, _person );

                //
                // Add our custom merge fields and generate the content.
                //
                mergeFields.Add( "Added", added );
                mergeFields.Add( "Removed", removed );
                nbSuccessMessage.Text = template.ResolveMergeFields( mergeFields );

                pnlGroupList.Visible = false;
                pnlGroupListKiosk.Visible = false;
                pnlGroupAttributes.Visible = false;
                pnlGroupAttributesKiosk.Visible = false;

                ScrollToControl( nbSuccessMessage );
            }

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "SaveRedirectPage" ) ) )
            {
                NavigateToLinkedPage( "SaveRedirectPage" );
            }
        }

        /// <summary>
        /// Trigger all configured workflows for the given list of membership records.
        /// </summary>
        /// <param name="membership">The list of GroupMember records that have been created or modified.</param>
        protected void TriggerWorkflows( List<GroupMember> membership )
        {
            WorkflowType workflowType = null;
            RockContext rockContext = new RockContext();
            WorkflowTypeService workflowTypeService = new WorkflowTypeService( rockContext );
            WorkflowService workflowService = new WorkflowService( rockContext );
            Guid? workflowTypeGuid = GetAttributeValue( "IndividualWorkflow" ).AsGuidOrNull();
            Workflow workflow;

            //
            // Process per-GroupMember workflow requests.
            //
            if ( workflowTypeGuid.HasValue )
            {
                workflowType = workflowTypeService.Get( workflowTypeGuid.Value );

                if ( workflowType != null && workflowType.Id != 0 )
                {
                    //
                    // Walk each GroupMember object and fire off a Workflow for each.
                    //
                    foreach ( var member in membership )
                    {
                        try
                        {
                            workflow = Workflow.Activate( workflowType, _person.FullName, rockContext );
                            if ( workflow != null )
                            {
                                List<string> workflowErrors;

                                workflowService.Process( workflow, member, out workflowErrors );
                            }
                        }
                        catch ( Exception ex )
                        {
                            ExceptionLogService.LogException( ex, this.Context );
                        }
                    }
                }
            }

            //
            // Activate an optional workflow for the entire submission.
            //
            workflowTypeGuid = GetAttributeValue( "SubmissionWorkflow" ).AsGuidOrNull();
            if ( workflowTypeGuid.HasValue )
            {
                workflowType = workflowTypeService.Get( workflowTypeGuid.Value );

                if ( workflowType != null && workflowType.Id != 0 )
                {
                    try
                    {
                        workflow = Workflow.Activate( workflowType, _person.FullName, rockContext );
                        if ( workflow != null )
                        {
                            List<string> workflowErrors;

                            if ( workflow.Attributes.ContainsKey( GetAttributeValue( "SubmissionAttribute" ) ) )
                            {
                                string guids = membership.Select( gm => gm.Guid.ToString() ).ToList().AsDelimited( "," );
                                workflow.SetAttributeValue( GetAttributeValue( "SubmissionAttribute" ), guids );
                            }
                            workflowService.Process( workflow, _person, out workflowErrors );
                        }
                    }
                    catch ( Exception ex )
                    {
                        ExceptionLogService.LogException( ex, this.Context );
                    }
                }
            }
        }

        /// <summary>
        /// Scroll the viewport so that the control is at the top of the screen.
        /// </summary>
        /// <param name="control">Control to scroll into the viewport.</param>
        protected void ScrollToControl( Control control )
        {
            string script = string.Format( "var bounds = document.getElementById('{0}').getBoundingClientRect(); if (bounds.top > window.innerHeight || bounds.bottom < 0) {{ $('html, body').animate({{ scrollTop: $('#{0}').offset().top - 10 }}, 250); }}", control.ClientID );

            ScriptManager.RegisterStartupScript( Page, GetType(), "ScrollToControl", script, true );
        }

        /// <summary>
        /// Populate the ddlSettingsSubmissionAttribute drop down with the Attributes available in
        /// the specified WorkflowType.
        /// </summary>
        /// <param name="workflowType">The WorkflowType whose Attributes should be populated.</param>
        protected void LoadSubmissionWorkflowAttributes( WorkflowType workflowType )
        {
            ddlSettingsSubmissionAttribute.Items.Clear();
            ddlSettingsSubmissionAttribute.Items.Add( string.Empty );

            if ( workflowType != null )
            {
                new AttributeService( new RockContext() )
                    .GetByEntityTypeId( new Workflow().TypeId ).AsQueryable()
                    .Where( a =>
                        a.EntityTypeQualifierColumn.Equals( "WorkflowTypeId", StringComparison.OrdinalIgnoreCase ) &&
                        a.EntityTypeQualifierValue.Equals( workflowType.Id.ToString() ) )
                    .OrderBy( a => a.Order )
                    .ThenBy( a => a.Name )
                    .ToList()
                    .ForEach( a => ddlSettingsSubmissionAttribute.Items.Add( a.Name ) );
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the btnSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSubmit_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            var membership = GetMembershipRecords( rockContext );
            var attributeMembership = GetAttributeMembershipRecords( membership, rockContext );
            List<ValidationResult> errorList = new List<ValidationResult>();

            //
            // Pre-check the membership list to make sure they are all valid.
            //
            foreach ( var member in membership )
            {
                if ( !member.IsValid )
                {
                    errorList.AddRange( member.ValidationResults );
                }
            }

            //
            // Check for any error message and display it. Also scroll the div onto the screen.
            //
            nbErrorMessage.Text = string.Empty;
            if ( errorList.Any() )
            {
                string errors = "Unable to complete request, the following errors prevented completing your selections:<br /><ul>";

                errors += errorList.Select( a => string.Format( "<li>{0}</li>", a.ErrorMessage ) ).ToList().AsDelimited( string.Empty );
                errors += "</ul>";

                nbErrorMessage.Text = errors;
                ShowGroups( false );

                ScrollToControl( nbErrorMessage );

                return;
            }

            //
            // If any of the groups have member attributes that we need to ask the user
            // about then show that information to the user before saving.
            //
            if ( attributeMembership.Any() )
            {
                pnlGroupList.Visible = false;
                pnlGroupListKiosk.Visible = false;
                pnlLavaDebug.Visible = false;
                pnlGroupAttributes.Visible = !_isKoiskMode;
                pnlGroupAttributesKiosk.Visible = _isKoiskMode;

                rptrGroupAttributes.Controls.Clear();
                rptrGroupAttributesKiosk.Controls.Clear();
                BindGroupMembership( true, attributeMembership );
            }
            else
            {
                RemoveFromUnselectedGroups( rockContext, membership );
                Save( rockContext );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnAttributesBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAttributesBack_Click( object sender, EventArgs e )
        {
            pnlGroupList.Visible = !_isKoiskMode;
            pnlGroupListKiosk.Visible = _isKoiskMode;
            pnlGroupAttributes.Visible = false;
            pnlGroupAttributesKiosk.Visible = false;

            ShowGroups( false );
        }

        /// <summary>
        /// Handles the Click event of the btnAttributesSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAttributesSubmit_Click( object sender, EventArgs e )
        {
            Repeater rptr = pnlGroupAttributes.Visible ? rptrGroupAttributes : rptrGroupAttributesKiosk;
            RockContext rockContext = new RockContext();
            var membership = GetMembershipRecords( rockContext );
            var attributeMembership = GetAttributeMembershipRecords( membership, rockContext );

            if ( attributeMembership.Count == rptr.Controls.Count )
            {
                //
                // This is a pretty serious hack, but since a placeholder is a really simple control it should
                // be safe to do this. We are accessing the contents of the PlaceHolder directly to find the
                // current values of the attributes.
                //
                for ( int i = 0; i < attributeMembership.Count; i++ )
                {
                    PlaceHolder phAttributes = ( PlaceHolder )rptr.Controls[i].FindControl( "phAttributes" );

                    Helper.GetEditValues( phAttributes, attributeMembership[i] );
                }

                RemoveFromUnselectedGroups( rockContext, membership );
                Save( rockContext );
            }
            else
            {
                nbErrorMessage.Text = "An unexpected error occurred trying to read your selections.";
                pnlGroupAttributes.Visible = false;
                pnlGroupAttributesKiosk.Visible = false;
                ScrollToControl( nbErrorMessage );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSettingsSave_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var group = new GroupService( rockContext ).Get( gpSettingsGroup.SelectedValue.AsInteger() );
            var cancelPage = new PageService( rockContext ).Get( ppCancelPage.SelectedValue.AsInteger() );

            SetAttributeValue( "Group", (group.Id != 0 ? group.Guid.ToString() : string.Empty) );
            SetAttributeValue( "GroupRole", (grpSettingsRole.GroupRoleId.HasValue ? grpSettingsRole.GroupRoleId.Value.ToString() : string.Empty) );
            SetAttributeValue( "AddAsStatus", ddlSettingsAddAsStatus.SelectedValue );
            SetAttributeValue( "RequestMemberAttributes", ddlSettingsRequestMemberAttributes.SelectedValue );
            SetAttributeValue( "CancelPage", cancelPage != null ? cancelPage.Guid.ToString() : string.Empty );

            SetAttributeValue( "AllowRemove", cbSettingsAllowRemove.Checked.ToString() );
            SetAttributeValue( "MinimumSelection", nbSettingsMinimumSelection.Text );
            SetAttributeValue( "MaximumSelection", nbSettingsMaximumSelection.Text );

            SetAttributeValue( "SaveRedirectPage", (ppSettingsSaveRedirectPage.PageId.HasValue ? new PageService( new RockContext() ).Get( ppSettingsSaveRedirectPage.PageId.Value ).Guid.ToString() : string.Empty) );
            SetAttributeValue( "IndividualWorkflow", (wtpSettingsIndividualWorkflow.SelectedValueAsId().HasValue ? new WorkflowTypeService( new RockContext() ).Get( wtpSettingsIndividualWorkflow.SelectedValueAsId().Value ).Guid.ToString() : string.Empty) );
            SetAttributeValue( "SubmissionWorkflow", (wtpSettingsSubmissionWorkflow.SelectedValueAsId().HasValue ? new WorkflowTypeService( new RockContext() ).Get( wtpSettingsSubmissionWorkflow.SelectedValueAsId().Value ).Guid.ToString() : string.Empty) );
            SetAttributeValue( "SubmissionAttribute", ddlSettingsSubmissionAttribute.SelectedValue );
            SetAttributeValue( "SavedTemplate", ceSettingsSavedTemplate.Text );

            SetAttributeValue( "SubmitTitle", tbSettingsSubmitTitle.Text );
            SetAttributeValue( "HeaderTitle", tbSettingsHeaderTitle.Text );
            SetAttributeValue( "KioskMode", cbSettingsKioskMode.Checked.ToString() );
            SetAttributeValue( "ContentTemplate", ceSettingsContentTemplate.Text );
            SetAttributeValue( "LavaDebug", cbSettingsLavaDebug.Checked.ToString() );

            SaveAttributeValues();

            mdSettings.Hide();
            pnlSettingsModal.Visible = false;

            _isKoiskMode = cbSettingsKioskMode.Checked;
            pnlGroupList.Visible = !_isKoiskMode;
            pnlGroupListKiosk.Visible = _isKoiskMode;
            pnlGroupAttributes.Visible = false;
            pnlGroupListKiosk.Visible = false;

            ShowGroups( true );
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            pnlGroupList.Visible = !_isKoiskMode;
            pnlGroupListKiosk.Visible = _isKoiskMode;
            pnlGroupAttributes.Visible = false;
            pnlGroupAttributesKiosk.Visible = false;

            ShowGroups( true );
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptrGroupAttributes control.
        /// </summary>
        /// <param name="sender">The object that sent the message.</param>
        /// <param name="e">Event arguments.</param>
        protected void rptrGroupAttributes_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            PlaceHolder phAttributes = e.Item.FindControl( "phAttributes" ) as PlaceHolder;

            Helper.AddEditControls( ( GroupMember )e.Item.DataItem, phAttributes, !_isGroupMembershipRebind, BlockValidationGroup, true );
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

        /// <summary>
        /// User has changed the value of the selected Group. Update the roles drop down with the new values.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void wtpSettingsSubmissionWorkflow_SelectItem( object sender, EventArgs e )
        {
            WorkflowType submissionWorkflowType = (wtpSettingsSubmissionWorkflow.SelectedValueAsId().HasValue ? new WorkflowTypeService( new RockContext() ).Get( wtpSettingsSubmissionWorkflow.SelectedValueAsId().Value ) : new WorkflowType());
            string selection = ddlSettingsSubmissionAttribute.SelectedValue;

            LoadSubmissionWorkflowAttributes( submissionWorkflowType );
            ddlSettingsSubmissionAttribute.SelectedValue = ddlSettingsSubmissionAttribute.Items.FindByValue( selection ) != null ? selection : string.Empty;
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "CancelPage" );
        }

        #endregion
    }
}
