function RenderQuizDisplay() {
    var value = $('[id*="hfConfiguration"]').val();

    if (!value) { return; }

    if (value.length < 2) {
        $('[id*="hfConfiguration"]').val("[]");
    }

    quiz = JSON.parse($('[id*="hfConfiguration"]').val());
    questionNumber = 0;
    score = 0;

    $('[id*="btnNext"]').hide();

    DisplayNextQuestion();
}

function DisplayNextQuestion() {
    

    var $content = $('.QuizContent').first();
    $content.empty();

    if (quiz.length > questionNumber) {
        var question = quiz[questionNumber];
        var questionType = question['question-type'];
        var answerStyle = 'single';
        if (questionType === 'Multi') {
            answerStyle = 'multi'
        }


        var $item = $("<div class='answers'><h2>Question " + (questionNumber + 1) + " of " + quiz.length + "</h2> "
            + question['question'] + "</div>");
        $content.append($item);
        var answers = question['answers'];
        for (var key in answers) {
            var cssClass = 'btn-default';
            var icon = '';

            var $answer = $("<div class='answer' data-id='" + answers[key] + "'><i class='fa fa-circle-o " + answerStyle + "'></i>" + key + "</span>");
            $answer.click(function () { SelectAnswer(this, questionType); });

            $item.append($answer);
        }


        var $next = $("<br><span class='btn btn-lg btn-primary disabled submit'>Next</span>");
        $next.click(function () {
            if (this.classList.contains('disabled')) {
                return;
            }
            ScoreQuestion()
        });
        $item.append($next);

        questionNumber++;
    }
    else {
        DisplayCompletion();
    }
}

function SelectAnswer(element, questionType) {
    if (questionType === 'Single') {
        $('.selected').removeClass('selected');
    }

    var $item = $(element);
    if (element.classList.contains('selected')) {
        $item.removeClass('selected');
    }
    else {
        $item.addClass('selected');
    }

    if ($('.selected').length > 0) {
        $('.submit').removeClass('disabled');
    }
    else {
        $('.submit').addClass('disabled');
    }
}

function ScoreQuestion() {
    var $content = $('.QuizContent').first();
    var $children = $content.find('.answer');
    var correct = true;

    $children.each(function () {
        var $answer = $(this);
        if ($answer.attr('data-id') === "1") {
            if ($answer.hasClass('selected')) {
                $answer.addClass('correct');
            }
            else {
                $answer.addClass('incorrect');
                correct = false;
            }
        }
        else {
            if ($answer.hasClass('selected')) {
                $answer.addClass('incorrect');
                correct = false;
            }
        }
        $answer.unbind();
    });

    var $submit = $('.submit');
        $submit.removeClass('btn-primary');

    if (correct) {
        $submit.text('Correct');
        $submit.addClass('btn-success');
        score++;
    }
    else {
        $submit.text('Incorrect');
        $submit.addClass('btn-danger');
    }

    $submit.unbind();
    $submit.click(function () { DisplayNextQuestion() })
}



function DisplayCompletion() {
    $('[id*="hfScore"]').val(score);
    var $content = $('.QuizContent').first();
    $content.empty();

    var $results = $("<div class='row'><div class='well'><h2>Quiz Complete</h2>You got " + score + " out of " + quiz.length + " correct.</div></div>");
    $content.append($results);

    $('[id*="btnNext"]').show();
}

