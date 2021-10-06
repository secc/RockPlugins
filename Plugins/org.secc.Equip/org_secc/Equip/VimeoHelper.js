function VimeoSetup() {
    try {
        clearInterval(progressInterval);
    }
    catch {
        //Nom nom nom
    }

    timeComplete = 0;

    var container = document.getElementById("vimeoVideoContainer");
    if (container === null) {
        return;
    }

    requiredScore = parseInt($('[id*="hfScore"]').val());
    if (requiredScore > 0) {
        $('[id*="btnNext"]').hide();
    }
    else {
        requiredScore = 0;
    }

    if (requiredScore > 95) {
        requiredScore = 95;
    }

    var iframe = container.firstChild;
    player = new Vimeo.Player(iframe);

    player.on('play', playStart);

    player.on('pause', playStop);
}

function playProgress() {
    var now = new Date();
    var milis = now.getTime();
    var diff = milis - playTime;
    playTime = milis;
    addTime(diff);
}

function addTime(time) {
    if (time < 0) {
        return;
    }
    timeComplete += time;
    var percent = (timeComplete / videoDuration) * 100;
    if (percent >= requiredScore) {

        var container = document.getElementById("vimeoVideoContainer");
        if (container === null) {
            return;
        }

        $('[id*="btnNext"]').show();
    }
}

function playStart() {
    var now = new Date();
    playTime = now.getTime();
    player.getDuration().then(function (duration) {
        videoDuration = duration * 1000;
    });
    progressInterval = setInterval(playProgress, 500);
}

function playStop() {
    clearInterval(progressInterval);
    playProgress();
}
