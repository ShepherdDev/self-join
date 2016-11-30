### Settings

![Settings](https://github.com/ShepherdDev/self-join/raw/master/Documentation/Settings.png)

#### General

* Group - The group to use as a root for showing selections. This
is passed to the Lava parser as a property called `Group` and can
be used to build the list of checkboxes or radio buttons.
* Role - The group role to use when adding new members to the
group. If the group does not have this role then the default role
will be used instead.
* Add As Status - The member status to add new members to the
group with. Group and Role capacities will only be enforced if
this is set to Active.
* Request Member Attributes - If a Group has Member Attributes
defined then you can set if you want those attributes to be filled
in by the user. If set to Required then the user will only be
prompted for Attributes that are marked as Required. If set to All
then the user will be prompted for all Attributes. Otherwise no
attributes will be prompted for.

#### Limitations

* Allow Remove - Allow a user to remove themselves from a group
they are already in. If No then any checkboxes will be disabled
for these groups. Radio buttons can still be deselected, however
the internal logic will not remove them from the group.
* Minimum Selection - The minimum number of selections that the
user must select before proceeding.
* Maximum Selection - The maximum number of checkboxes that the
user can select. Has no effect on radio buttons. Set to 0 for
unlimited.

#### Post-Save Actions

* Save Redirect Page - The page to redirect the user to after all
their changes have been saved.
* Individual Workflow - Activate the selected workflow for each
individual GroupMember record created (also fires if an GroupMember
changes from Inactive to Pending or Active). The GroupMember is
passed as the Entity to the workflow.
* Submission Workflow - Activate the selected workflow one time
for each submission. The CurrentPerson is passed as the Entity to
the workflow.
* Submission Attribute - Attribute to store the group member GUIDs
into as a comma separated list.
* Saved Template - Message to be displayed to the user once all
their selections have been saved. Lava objects `Added` and
`Removed` are arrays of GroupMember objects for the groups they
were added or removed from.

#### User Interface

* Submit Title - Title of the Submit button to show to the user.
* Content Template - Template to use for the content that
generates the checkboxes or radio buttons. Any checkbox or radio
button will automatically be selected and enabled/disabled as
needed. The Lava property `Name` can be used as a unique name key
for the input controls though it is not required to match. If a
checkbox or radio button is disabled or enabled then a jQuery
`disabled` and `enabled` event will be triggered allowing you
to do custom UI updates.
* Lava Debug - Show the Lava Debug panel which contains detailed
information about what fields are available in the Content
Template.

### Usage

Out of the box, all you need to do is select a parent Group and
set the desired role. The default Lava `Content Template` will
give you an indented list of checkboxes for each child group up
to 3 levels deep, not including the parent Group. The underlying
javascript will handle both checkboxes as well as radio buttons.
So if you wanted to only allow one selection you could switch it
to radio buttons instead (or simply set the `Maximum Selection`
to 1).

For more advanced usage, you could customize the lava to use the
first level of groups as categories and then the second level
of groups as radio selection inside each category.

### On Save

If no redirect page is set then the user will simply see the
`Saved Template` message, otherwise they will be redirected to
the selected page.

If you specify a workflow for the `Individual Workflow`
setting then a workflow will be launched for every group that the
user is added to (or activated in, in the case of already being
in the group but marked as Inactive). So if the user picks 3
checkboxes then the workflow will be launched 3 times. Each
workflow will be be passed the GroupMember object as the Entity
which can be used in the workflow to identify the person and the
group they joined.

Specifying a workflow for the `Submission Workflow` will cause
a single instance of that workflow to fire for the entire session.
The list of GroupMember records can optionally be passed to the
workflow via the attribute specified in the `Submission
Attribute` setting. The list is passed as a string of GUIDs,
each separated by a comma.