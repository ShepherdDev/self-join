(function ($) {
    window.SelfJoin = function (ClientID, HiddenField, MinimumSelection, MaximumSelection, LockedValues) {
        this.clientID = ClientID;
        this.hiddenField = HiddenField;
        this.minCount = MinimumSelection;
        this.maxCount = MaximumSelection;
        this.lockedValues = LockedValues.split(',');
        this.originalValues = ($('#' + this.hiddenField).val() ? $('#' + this.hiddenField).val().split(',') : []);

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
                $('#' + this.clientID + ' .js-save-button').prop('disabled', false);
            }
            else {
                $('#' + this.clientID + ' .js-save-button').prop('disabled', true);
            }

            this.updateHiddenField();
        }

        //
        // Store the list of current selections into a hidden field.
        //
        this.updateHiddenField = function() {
            var selected = [];
            var _this = this;
            
            /* If the admin has not included a checkbox/radio for this original value then include it anyway. */
            this.originalValues.forEach(function (v) {
                if ($('#' + _this.clientID + ' input[value="' + v + '"]').length == 0)
                {
                    selected.push(v);
                }
            });

            /* Find the user's selections. */
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
            var selectedValues = this.originalValues;
            var _this = this;

            /* By default click to tic any value="" non-checked inputs. */
            $('#' + this.clientID + ' input[type="checkbox"][value=""]:not(:checked), #' + this.clientID + ' input[type="radio"][value=""]:not(:checked)').click();

            /* Now go through and tic any inputs that should be selected based on existing values. */
            $('#' + this.clientID + ' input[type="checkbox"]:not([value=""]), #' + this.clientID + ' input[type="radio"]:not([value=""])').each(function () {
                if ($(this).prop('checked') != (selectedValues.indexOf($(this).attr('value')) != -1)) {
                    $(this).click();
                }

                if (_this.lockedValues.indexOf($(this).attr('value')) != -1) {
                    if ($(this).prop('checked') == false) {
                        $(this).click();
                    }
                    $(this).prop('disabled', true).triggerHandler('disabled');
                }
            });

            $('#' + this.clientID + ' input[type="checkbox"]').click(function () { this.updateStates(); }.bind(this));
            $('#' + this.clientID + ' input[type="radio"]').click(function () { this.updateStates(); }.bind(this));

            this.updateStates();
        }

        this.setup();
    }
})(jQuery);
