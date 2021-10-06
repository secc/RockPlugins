var player;
ytReady = false;

function YouTubeSetup() {
    try {
        clearInterval(progressInterval);
    }
    catch {
        //Nom nom nom
    }

    requiredScore = parseInt($('[id*="hfScore"]').val());

    if (requiredScore > 0) {
        $('[id*="btnNext"]').hide();
    }
    else {
        requiredScore = 0;
    }

    timeComplete = 0;

    var container = document.getElementById("youtubeVideoContainer");
    if (!container) { return; }
    var iframe = container.firstChild;
    iframe.id = "youtubePlayer";

    var tag = document.createElement('script');

    tag.src = "https://www.youtube.com/iframe_api";
    var firstScriptTag = document.getElementsByTagName('script')[0];
    firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

    CreateYoutubePlayer();
}

function CreateYoutubePlayer() {
    if (!ytReady) {
        setTimeout(CreateYoutubePlayer, 100);
        return;
    }

    player = new YT.Player('youtubePlayer', {
        events: {
            'onStateChange': onPlayerStateChange
        }
    });
}

function onYouTubeIframeAPIReady() {
    ytReady = 1;
}

function updateStatus(playerStatus) {
    if (playerStatus === 0) {
        playStop(); // ended 
    } else if (playerStatus === 1) {
        playStart(); // playing 
    } else if (playerStatus === 2) {
        playStop(); // paused
    }
}

function onPlayerStateChange(event) {
    updateStatus(event.data);
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
        $('[id*="btnNext"]').show();
    }
}

function playStart() {
    videoDuration = player.getDuration() * 1000;
    var now = new Date();
    playTime = now.getTime();
    progressInterval = setInterval(playProgress, 500);
}

function playStop() {
    clearInterval(progressInterval);
    playProgress();
}
