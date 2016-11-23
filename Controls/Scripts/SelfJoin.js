(function ($) {
    window.SelfJoin = function (ClientID, HiddenField, SaveButton, MinimumSelection, MaximumSelection, LockedValues) {
        this.clientID = ClientID;
        this.hiddenField = HiddenField;
        this.saveButton = SaveButton;
        this.minCount = MinimumSelection;
        this.maxCount = MaximumSelection;
        this.lockedValues = LockedValues.split(',');

        //
        // Update the enabled/disabled states of all check boxes. Also updates the hidden field
        // that contains the list of current selections.
        //
        this.updateStates = function () {
            var checkedCount = $('#' + this.clientID + ' input[type="checkbox"]:checked:not([value=""]), #' + this.clientID + ' input[type="radio"]:checked:not([value=""])').length;

            /* Unselect any non-locked checkboxes */
            if (this.minCount > 0 || this.maxCount > 0) {
                var _this = this;
                $('#' + this.clientID + ' input[type="checkbox"]').each(function () {
                    if (_this.lockedValues.indexOf($(this).attr('value')) == -1) {
                        $(this).prop('disabled', false).triggerHandler('enabled');
                    }
                });
            }

            /* If we are at or above the maximum selection count, disable any unselected boxes. */
            if (this.maxCount > 0 && checkedCount >= this.maxCount) {
                $('#' + this.clientID + ' input[type="checkbox"]:not(:checked)').each(function () {
                    $(this).prop('disabled', true).triggerHandler('disabled');
                });
            }

            /* If we are within the allowed numbers, enable the save button. */
            if ((this.minCount == 0 || checkedCount >= this.minCount) && (this.maxCount == 0 || checkedCount <= this.maxCount)) {
                $('#' + this.saveButton).prop('disabled', false);
            }
            else {
                $('#' + this.saveButton).prop('disabled', true);
            }

            this.updateHiddenField();
        }

        //
        // Store the list of current selections into a hidden field.
        //
        this.updateHiddenField = function() {
            var selected = [];

            $('#' + this.clientID + ' input[type="checkbox"]:not([value=""]), #' + this.clientID + ' input[type="radio"]:not([value=""])').each(function () {
                if ($(this).prop('checked') && selected.indexOf($(this).attr('value')) == -1) {
                    selected.push($(this).attr('value'));
                }
            });

            $('#' + this.hiddenField).val(selected.join(','));
        }

        //
        // Setup click handlers and set initial selections.
        //
        this.setup = function() {
            var selectedValues = $('#' + this.hiddenField).val().split(',');
            var _this = this;

            $('#' + this.clientID + ' input[type="checkbox"][value=""], #' + this.clientID + ' input[type="radio"][value=""]').prop('checked', true);

            $('#' + this.clientID + ' input[type="checkbox"]:not([value=""]), #' + this.clientID + ' input[type="radio"]:not([value=""])').each(function () {
                $(this).prop('checked', selectedValues.indexOf($(this).attr('value')) != -1);

                if (_this.lockedValues.indexOf($(this).attr('value')) != -1) {
                    $(this).prop('disabled', true).prop('checked', true).triggerHandler('disabled');
                }
            });

            $('#' + this.clientID + ' input[type="checkbox"]').click(function () { this.updateStates(); }.bind(this));
            $('#' + this.clientID + ' input[type="radio"]').click(function () { this.updateStates(); }.bind(this));

            this.updateStates();
        }

        this.setup();
    }
})(jQuery);
