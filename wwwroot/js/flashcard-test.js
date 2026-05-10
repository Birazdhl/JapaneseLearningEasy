/**
 * AJAX-driven reviewer for English ↔ Japanese drills.
 */
(function ($) {
    "use strict";

    function ajaxHeaders(additionalHeaders) {
        const tokenMeta = document.querySelector('meta[name="request-verification-token"]');
        const token = tokenMeta ? tokenMeta.getAttribute("content") : "";

        const headers = Object.assign({}, additionalHeaders, {
            RequestVerificationToken: token
        });

        return headers;
    }

    function postAjax(url, body) {
        return $.ajax({
            url: url,
            method: "POST",
            headers: ajaxHeaders({
                "Content-Type": "application/json"
            }),
            data: body ?? "{}",
            cache: false
        });
    }

    function applyProgress(progress) {
        if (!progress) {
            $("#quiz-progress-copy").text("0 of 0 completed");
            $(".jl-progress-accent").css("width", "0%").attr("aria-valuenow", 0);
            return;
        }

        var total = parseInt(progress.total, 10) || 0;
        var completed = parseInt(progress.completed, 10) || 0;
        var pct = total === 0 ? 0 : Math.min(100, Math.round((completed / total) * 100));
        $("#quiz-progress-copy").text(completed + " of " + total + " completed");
        $(".jl-progress-accent").css("width", pct + "%").attr("aria-valuenow", pct);

        $("#quiz-complete-pill").toggleClass("d-none", !progress.finished);
    }

    function resetAnswerUi(kind) {
        $("#quiz-answer-pane").removeClass("jl-reveal-visible");
        $("#quiz-mark-group").toggleClass("d-none", true);
        $("#quiz-show-answer-btn").toggleClass("d-none", false);

        if (kind === "english") {
            $("#quiz-romaji-row").removeClass("d-none");
        } else {
            $("#quiz-romaji-row").addClass("d-none");
        }
    }

    function renderPrompt(word, kind) {
        if (!word) {
            $("#quiz-prompt-body").html('<span class="text-muted text-center">No cards available.</span>');
            $("#quiz-meta-id").text("—");
            return;
        }

        $("#quiz-prompt-body").text(word.prompt || "");
        $("#quiz-meta-id").text("Word #" + word.id);

        if (kind === "english") {
            $("#quiz-meaning-label").text("Japanese");
            $("#quiz-meaning-reveal").text(word.secondaryReveal || "");
            $("#quiz-romaji-reveal").text(word.romajiReveal || "");
        } else {
            $("#quiz-meaning-label").text("English");
            $("#quiz-meaning-reveal").text(word.secondaryReveal || "");
            $("#quiz-romaji-reveal").text("");
        }
    }

    function hydrateResponse(response, quiz) {
        if (!response || response.success !== true) {
            var fallback = response && response.message ? response.message : "Unexpected quiz error.";
            toastr.error(fallback);
            quiz.$working.toggleClass("d-none", !(response && response.progress && Number(response.progress.total) > 0));
            quiz.$complete.removeClass("d-none");
            $(".quiz-complete-message").text(fallback);

            applyProgress(response && response.progress ? response.progress : null);
            return;
        }

        applyProgress(response.progress);

        if (!response.next) {
            var summary =
                response.message ||
                "Great job! Keep this cadence rolling into your next immersion block.";
            quiz.$complete.removeClass("d-none");
            quiz.$working.addClass("d-none");
            $(".quiz-complete-message").text(summary);
            toastr.success(summary);
            return;
        }

        quiz.$complete.addClass("d-none");
        quiz.$working.removeClass("d-none");

        renderPrompt(response.next, quiz.kind);
        resetAnswerUi(quiz.kind);
        quiz.currentWordId = response.next.id;
    }

    function bootstrapProgress(endpoints, quiz, done) {
        $.getJSON(endpoints.progressUrl)
            .always(function () {
                if (done) {
                    done();
                }
            })
            .done(function (payload) {
                applyProgress(payload);
            });
    }

    function initQuiz(kind, endpoints) {
        var $scope = $(".jl-quiz-root");
        var quiz = {
            kind: kind,
            endpoints: endpoints,
            currentWordId: null,
            $scope: $scope,
            $working: $(".quiz-working"),
            $complete: $(".quiz-complete")
        };

        function chainRestart() {
            quiz.$restart.prop("disabled", true);
            postAjax(endpoints.restartUrl)
                .always(function () {
                    quiz.$restart.prop("disabled", false);
                })
                .done(function (payload) {
                    hydrateResponse(payload, quiz);
                    bootstrapProgress(endpoints, quiz);
                })
                .fail(function () {
                    toastr.error("Unable to restart the quiz right now.");
                });
        }

        quiz.$restart = $(".quiz-restart");
        quiz.$pane = $("#quiz-answer-pane");
        quiz.$right = $(".quiz-mark-right");
        quiz.$wrong = $(".quiz-mark-wrong");

        quiz.$restart.on("click", function () {
            chainRestart();
        });

        $("#quiz-show-answer-btn").on("click", function () {
            $("#quiz-mark-group").toggleClass("d-none", false);
            $(this).toggleClass("d-none", true);
            window.requestAnimationFrame(function () {
                quiz.$pane.addClass("jl-reveal-visible");
            });
        });

        quiz.$right.on("click", function () {
            postAjax(endpoints.markRightUrl)
                .done(function (payload) {
                    hydrateResponse(payload, quiz);
                })
                .fail(function () {
                    toastr.error("Could not sync your answer. Reload to continue.");
                });
        });

        quiz.$wrong.on("click", function () {
            postAjax(endpoints.markWrongUrl)
                .done(function (payload) {
                    hydrateResponse(payload, quiz);
                })
                .fail(function () {
                    toastr.error("Could not sync your answer. Reload to continue.");
                });
        });

        chainRestart();
        bootstrapProgress(endpoints, quiz);
    }

    window.initFlashCardQuiz = initQuiz;
})(jQuery);
