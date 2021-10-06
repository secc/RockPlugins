function AddQuizItem(questionText, type) {

    if (!type) {
        type = "Single";
    }

    var titleText = questionText;
    if (!titleText) {
        titleText = "New Question";
    }

    var $content = $('.QuizContent');

    var $panel = $('<div data-id="' + itemId + '" class="panel panel-default"></div>');
    $content.append($panel);

    var $header = $('<div class="panel-heading" role="tab" id="heading-' + itemId + '"></div>');
    $panel.append($header);

    var $deleteQuestion = $("<a class='btn btn-danger pull-right btn-xs' style='margin-left:15px;'><i class='fa fa-times'></i></a>");
    $deleteQuestion.click(function (e) {
        e.preventDefault();
        bootbox.dialog({
            message: 'Are you sure you want to delete this question?',
            buttons: {
                ok: {
                    label: 'OK',
                    className: 'btn-primary',
                    callback: function () {
                        $panel.remove(); StoreConfiguration();
                    }
                },
                cancel: {
                    label: 'Cancel',
                    className: 'btn-default'
                }
            }
        });
    });
    $header.append($deleteQuestion);

    var $title = $('<a role="button" data-toggle="collapse" data-parent="#accordion-quizeditor" href="#collapse' + itemId + '-accordion-quizeditor" aria-expanded="true" aria-controls="collapse' + itemId + '">' +
        '<h4 class="panel-title" style="color:black">' +
        titleText +
        '<i class="fa fa-chevron-down pull-right"></i>' +
        '</h4>' +
        '</a>');
    $header.append($title);

    var expand = "";
    if (itemId === expandedId) {
        expand = "in";
    }

    var $body = $('<div id="collapse' + itemId + '-accordion-quizeditor" class="panel-collapse collapse ' + expand + '" role="tabpanel" aria-labelledby="heading-' + itemId + '" data-item-id="' + itemId + '">' +
        +'</div>');
    $panel.append($body);

    var $item = $('<div class="panel-body"></div>');
    $body.append($item);

    var $typeSelector = $("<div class='form-group rock-text-box'><label>Question Type</label><div class='control-wrapper'>" +
        "<select class='form-control question-type'>" +
        "<option value='Single'>Single Choice</option>" +
        "<option value='Multi'>Multiple Choice</option>" +
        "</select></div>");
    $item.append($typeSelector);

    var $selector = $typeSelector.find('.question-type');
    $selector.val(type);
    $selector.change(function () {
        StoreConfiguration();
        RenderQuizEditor();
    });


    var $questionText = $("<div class='form-group rock-text-box'><label>Question Text</label><div class='control-wrapper'>"
        + "<textarea  class='form-control question' placeholder='Question' rows='4' style='width:100%' ></textarea>" +
        "</div></div>");
    $questionText.find('.question').val(questionText);
    $questionText.change(function () { StoreConfiguration(); });
    $item.append($questionText);

    $item.append("<label>Answers:</label>");

    var $answers = $("<div style='margin-top:10px' data-type='" + type + "' class='answers'></div>");
    $answers.attr("data-id", Math.ceil(Math.random() * 100000));
    $item.append($answers);

    var $addAnswer = $("<a class='btn btn-default btn-sm'><i class='fa fa-plus'></i> Add Answer</a>");
    $addAnswer.click(function () { AddAnswer($answers); });
    $item.append($addAnswer);

    return $answers;
}

function AddAnswer($answers, checked, input) {
    var type = $answers.attr("data-type");
    if (type !== "Single" && type !== "Multi") {
        type = "Single";
    }

    var $answerBox = $("<div class='form-group form-inline-all'></div>");
    $answers.append($answerBox);

    var $label = $("<label class='answer-container'></label>");
    $answerBox.append($label);

    if (type === "Single") {
        var $radio = $("<input class='hidden' type='radio'><span class='radiobutton'></span>");
        $radio.change(function () { StoreConfiguration(); });
        $radio.attr('name', $answers.attr("data-id"));
        $radio.prop('checked', checked);
        $label.append($radio);
    } else {
        var $check = $("<input class='hidden' type='checkbox'><span class='checkmark'></span>");
        $check.change(function () { StoreConfiguration(); });
        $check.attr('name', $answers.attr("data-id"));
        $check.prop('checked', checked);
        $label.append($check);
    }

    var $input = $("<input class='form-control answer-text' style='width:50%' placeholder='Answer'>");
    $input.change(function () { StoreConfiguration(); });
    $input.val(input);
    $answerBox.append($input);

    var $removeAnswer = $("<a class='btn btn-link'><small>Remove Answer</small></a>");
    $removeAnswer.click(function () { $answerBox.remove(); StoreConfiguration(); });
    $answerBox.append($removeAnswer);
}

function StoreConfiguration() {
    var questions = [];
    var $content = $('.QuizContent').first();
    for (var i = 0; i < $content.children().length; i++) {
        var question = {};
        var $item = $content.children().eq(i);
        question['question'] = $item.find('.question').val();
        question['question-type'] = $item.find('.question-type').val();

        var $answers = $item.find('.answers');
        var answers = {};
        for (var j = 0; j < $answers.children().length; j++) {
            var $answerBox = $answers.children().eq(j);
            var score = 0;
            if ($answerBox.find('.hidden').is(':checked')) {
                score = 1;
            }
            else {
                score = 0;
            }
            answers[$answerBox.find('.answer-text').val()] = score;
        }
        question['answers'] = answers;
        questions.push(question);
    }

    $('[id*="hfConfiguration"]').val(JSON.stringify(questions));
}

function RenderQuizEditor() {

    //Add click handler to button
    $('#btnAddQuestion').unbind("click");

    $('#btnAddQuestion').click(function () {
        itemId++;
        var $answers = AddQuizItem();
        AddAnswer($answers, true);
    });

    var value = $('[id*="hfConfiguration"]').val();
    if (value.length < 2) {
        $('[id*="hfConfiguration"]').val("[]");
    }

    expandedId = parseInt($('.collapse.in').attr('data-item-id'));

    $('.QuizContent').empty();

    itemId = 0;

    var questions = JSON.parse($('[id*="hfConfiguration"]').val());
    for (var i = 0; i < questions.length; i++) {
        itemId++;
        var item = questions[i];
        var $answers = AddQuizItem(item['question'], item['question-type']);
        var answers = item['answers'];
        for (var key in answers) {
            AddAnswer($answers, answers[key] === 1, key);
        }
    }
}
