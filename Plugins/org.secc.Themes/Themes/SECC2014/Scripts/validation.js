/* global moment */

function validEmail(email) {
	//An empty email is valid.
	//If you want to check for empty, call the hasValue() method.
	if (email === "") {
		return true;
	}

    var re = /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(email);
}

function validPhone(phone) {
	//An empty phone is valid.
	//If you want to check for empty, call the hasValue() method.
	if (phone === "") {
		return true;
	}

    var re = /^\D*(\d\D*){10}$/;
    return re.test(phone);
}


function validDate(dateString) {
	var dateParts = dateString.split("/");
	if (dateParts.length < 3) {
		return false;
	}

	for(var i=0;i<dateParts.length;i++) {
		if (!$.isNumeric(dateParts[i]) || parseInt(dateParts[i]) === 0) {
			return false;
		}
	}

	var minDate = moment("1/1/1900", "M/D/YYYY");
	var maxDate = moment();
	var m = moment(dateString, "M/D/YYYY");

	return (
		m.isValid() &&
		(m.isSame(minDate, "day") || m.isBetween(minDate, maxDate, "day"))
	);
}

function hasValue(text) {
	return (text != null && text.trim() !== "");
}

function validateField($field) {
		var success = true;
		var tests;
		var i;
		if ($field.prop('type').toLowerCase() === "checkbox") {
			success = validateCheck($field, $($field[0].form));
		} else {
			tests = getTests($field);
			for(i=0;i<tests.length;i++) {
				switch(tests[i]) {
					case "required":
						success = hasValue($field.val());
						break;
					case "email":
						success = validEmail($field.val());
						break;
					case "phone":
						success = validPhone($field.val());
						break;
					case "custom":
						success = window[$field.data('validationFunction')]($field.val(), $field);
						break;
					case "date":
						success = validDate($field.val());
				}
				if (!success) {
					break; //Exit the for loop. No need to continue if we've got an error.
				}
			}

			processFieldResult($field, success, tests[i]);
		}

		return success;
}

function validateCheck($field, $form) {
	var success =true;
	var tests = getTests($field);
	var fieldName = $field.prop("name");

	for(var i=0;i<tests.length;i++) {
		switch(tests[i]) {
			case "required":
				success = ($form.find("input[name='" + fieldName + "']:checked").length > 0);
				break;
			case "require-this":
				success = $field.is(":checked");
				break;
		}
		if (!success) {
			break; //Exit the for loop. No need to continue if we've got an error.
		}
	}

	processFieldResult($field, success);
	return success;
}

function processFieldResult($field, success, validationType) {
	var error = getErrorField($field);
	if (error !== null) {
		var $errorField = $("#" + error);
		if (success) {
			$errorField.hide();
		} else {
			if ($errorField.data(validationType + "Error") !== undefined) {
				$errorField.html($errorField.data(validationType + "Error"));
			}
			$errorField.show();
		}
	} else {
		if (success) {
			$field.removeClass('error');
		} else if ($field.hasClass('error') === false) {
			$field.addClass('error');
		}
	}
}

function getTests($field) {
 		//Stores the valdation tests to check for this element.
		var tests = $field.data("validate");

		tests = tests.replace(/ /g, "");
		return tests.split(",");
}

function getErrorField($field) {
	return $field.data("errorField") || null;
}

/* exported toggleErrorField */
function toggleErrorField(fieldID, success) {
	if (fieldID == null) {
		return;
	}
	if (!success) {
		//This element has errors, so show it's error message.
		$("#" + fieldID).show();
	} else {
		//no errors, hide the error field
		$("#" + fieldID).hide();
	}

}


function validateForm($form) {
	var success = true;

	//Get all form fields with a data-validate attribute
	$form.find("*[data-validate]").each(function() {
		var $field = $(this);
		if (!validateField($field)) {
			success = false;
		}
	});

	if ($form.data("forceSubmit") === true) {
		success = true;
	}

	return success;
}

$(document).ready(function() {

	$("form.validate").submit(function(e) {
		var $form = $(this);
		var errorField = getErrorField($form);
		if ($form.hasClass("async")) {
			//Do nothing, the async handler will handle validation
			return;
		}

		if (!validateForm($form)) {
			toggleErrorField(errorField, false);
			e.preventDefault();
		} else {
			toggleErrorField(errorField, true);
		}
	});

	$("form.validate *[data-validate]:not([data-submit-only])").blur(function() {
		validateField($(this));
	});

	$("form.validate select[data-validate]:not([data-submit-only])").change(function() {
		validateField($(this));
	});

	$('form.async').submit(function(e) {
		e.preventDefault();

		var $form = $(this);
		var errorField = getErrorField($form);

		if ($form.hasClass("validate") && !validateForm($form)) {
			//Form isn't valid, don't submit;
			toggleErrorField(errorField, false);
			return;
		} else {
			toggleErrorField(errorField, true);
		}

		if ($form.data("simulateError") === true) {
			$(this).trigger('ajax:error', [500, "Internal Server Eror - Simulated"]);
			return;
		}

		var $indicator = $form.find('.form-processing');
		$indicator = ($indicator.length < 1) ? null : $indicator;
		if ($indicator !== null) {
			$indicator.show();
		}

		$.ajax({
			url: $form.prop('action'),
			type: $form.prop('method').toUpperCase(),
			data: $form.serialize(),
			context: this, //return the form itself in the callback.
			success: function(data) {
				if ($indicator !== null) {
					$indicator.hide();
				}

				$(this).trigger('ajax:success', [data]);
			},
			error: function(xhr, status, error) {
				console.log(xhr);
				console.log(status);
				console.log(error);

				if ($indicator !== null) {
					$indicator.hide();
				}

				$(this).trigger('ajax:error', [status, error, xhr.responseText]);
			}
		});
	});

});

/* exported DobValidator */
function DobValidator($wrapper) {
	this.wrapper = $wrapper;
	this.reset();

	var self = this;
	this.wrapper.find("input").on('keyup', function(e) {
		var $field = $(this);
		setTimeout(function() {
			self.onKeyPress($field, e);
		}, 100);
	});

	this.wrapper.find("input").on('keydown', function(e) {
		var key = e.keyCode || e.charCode;

		// Allow: backspace, delete, tab, escape, enter and .
        if ($.inArray(key, [46, 8, 9, 27, 13, 110, 190]) !== -1 ||
             // Allow: Ctrl+A, Command+A
            (key === 65 && ( e.ctrlKey === true || e.metaKey === true ) ) ||
             // Allow: home, end, left, right, down, up
            (key >= 35 && key <= 40)) {
                 // let it happen, don't do anything
                 return;
        }

        // Ensure that it is a number and stop the keypress
        if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
            e.preventDefault();
        }
	});


}

DobValidator.prototype.onKeyPress = function($field, event) {

	this.wrapper.find(".formatted-date").val(this.getFieldValue(this.getMonth()) + "/" + this.getFieldValue(this.getDay()) + "/" + this.getFieldValue(this.getYear()));

	var key = event.keyCode || event.charCode;

	switch (key) {
		case 8:
		case 46:
			this.handleBackspace($field);
			break;

		case 189:
		case 190:
		case 191:
		case 111:
		case 109:
		case 110:
			this.handleSeparator($field);
			break;

		case 48:
		case 49:
		case 50:
		case 51:
		case 52:
		case 53:
		case 54:
		case 55:
		case 56:
		case 57:
		case 96:
		case 97:
		case 98:
		case 99:
		case 100:
		case 101:
		case 102:
		case 103:
		case 104:
		case 105:
			this.handleNumber($field);
			break;

		default:
			break;
	}
};

DobValidator.prototype.reset = function() {
	this.wrapper.find(".date-input").val("");
	this.wrapper.find(".formatted-date").val("");
};

DobValidator.prototype.getMonth = function() {
	return this.wrapper.find(".month");
};

DobValidator.prototype.getDay = function() {
	return this.wrapper.find(".day");
};

DobValidator.prototype.getYear = function() {
	return this.wrapper.find(".year");
};

DobValidator.prototype.getFieldValue = function($field) {
	return ($.isNumeric($field.val())) ? parseInt($field.val()) : 0;
};

DobValidator.prototype.getFieldType = function($field) {
	if ($field.hasClass("month")) {
		return "month";
	}

	if ($field.hasClass("day")) {
		return "day";
	}

	if ($field.hasClass("year")) {
		return "year";
	}
};

DobValidator.prototype.hasFullDate = function() {
	var self = this;
	var hasDate = true;
	this.wrapper.find(".date-input").each(function() {
		if (self.getFieldValue($(this)) === 0) {
			hasDate = false;
		}
	});
	return hasDate;
};

DobValidator.prototype.getNextField = function($field) {

	if (this.getFieldType($field) === "month") {
		return this.getDay();
	} else if (this.getFieldType($field) === "day") {
		return this.getYear();
	}

	return null;
};


DobValidator.prototype.getPrevField = function($field) {

	if (this.getFieldType($field) === "year") {
		return this.getDay();
	} else if (this.getFieldType($field) === "day") {
		return this.getMonth();
	}

	return null;
};

DobValidator.prototype.validateDate = function() {

	var dateString = this.wrapper.find(".formatted-date").val();
	var dateParts = dateString.split("/");
	if (dateParts.length < 3) {
		return false;
	}

	for(var i=0;i<dateParts.length;i++) {
		if (!$.isNumeric(dateParts[i]) || parseInt(dateParts[i]) === 0) {
			return false;
		}
	}

	var minDate = moment("1/1/1900", "M/D/YYYY");
	var maxDate = moment().subtract(2, 'years');
	var m = moment(dateString, "M/D/YYYY");

	return (
		m.isValid() &&
		(m.isSame(minDate, "day") || m.isBetween(minDate, maxDate, "day"))
	);
};


DobValidator.prototype.validateField = function($field) {
	var m; //moment;
	var formatString = "";
	var inputString = "";
	var dateObj = {
		M: this.getMonth(),
		D: this.getDay(),
		YYYY: this.getYear()
	};

	for(var key in dateObj) {
		$field = dateObj[key];
		if (this.getFieldValue($field) > 0) {
			formatString += "/" + key;
			inputString += "/" + this.getFieldValue($field);
		}
	}

	m = new moment(inputString, formatString);
	return ( m.isValid() );
};


DobValidator.prototype.handleBackspace = function($field) {
	var $moveTo = this.getPrevField($field);

	if ($moveTo === null || $field.val() !== "") {
		return;
	}

	$moveTo.focus();
};

DobValidator.prototype.handleSeparator = function($field) {
	var $moveTo = this.getNextField($field);

	if (this.validateField($field) && $moveTo !== null) {
		$moveTo.focus();
	}
};

DobValidator.prototype.handleNumber = function($field) {
	if (!this.validateField($field)) {
		return;
	}

	var $moveTo = null;
	var value = parseInt($field.val());

	switch(this.getFieldType($field)) {
		case "month":
			if (value > 1 || $field.val().length === 2) {
				$moveTo = this.getDay();
			}
			break;

		case "day":
			if (value > 3 || $field.val().length === 2) {
				$moveTo = this.getYear();
			}
			break;
	}

	if ($moveTo !== null) {
		$moveTo.focus();
	}
};
