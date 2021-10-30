// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

var errorModal = $("#errorModal");
var errorModalBody = errorModal.find('.modal-body');
const targetModalId = "#pageModal";

function pageModal(e, data = null) {
    e.preventDefault();
    
    // Add query so the page knows to render in the modal view layout
    var urlParams = new URLSearchParams(e.srcElement.search);
    urlParams.set("modalView", "true");
    updatedParams = urlParams.toString()
    targetUrl = e.srcElement.pathname + "?" + updatedParams;

    var targetModal = $(targetModalId);
    var modalBody = targetModal.find('.modal-body');
    console.log("Opening modal for:", targetUrl)

    $.ajax({
        type: "GET",
        url: targetUrl,
        data: data,
        contentType: "application/json; charset=utf-8",
        dataType: "html",
        success: function (response) {
            targetModal.modal('toggle');
            modalBody.html(response);
            updateModalPageForms(targetModal);
        },
        error: function (response) {
            errorModal.modal('toggle');
            var ret = response.responseText ? response.responseText : response;
            errorModalBody.html(ret);
            console.log(ret)
        }
    });
}

function updateModalPageForms(targetModal) {
    // Update forms and buttons in the modal to perform correct actions

    var modalBody = targetModal.find('.modal-body');
    var registerBody = $("#register");
    var registerForm = modalBody.find("#registerForm");
    var mainForm = modalBody.find('form').not("#registerForm");

    // Update the search params so re-renders have the correct view layout for modal as per _viewStart.cshtml
    var action = mainForm.attr('action');
    if (!action.includes('modalView')) {
        var newAction =
            action.includes('?') ?
                action + '&modalView=true' : action + '?modalView=true';
        mainForm.attr('action', newAction);
    }

    if (registerForm.length > 0) {
        let submitButton = modalBody.find("#submitButton");
        var ev = $._data(submitButton[0], 'events');
        if (!ev && !ev?.click) {
            submitButton.on('click', e => submitBoth(e, targetModal, targetUrl))
        }
    } else {
        var ev = $._data(mainForm[0], 'events');
        if (!ev && !ev?.submit) {
            mainForm.on('submit', e => submitForm(e, mainForm, modalBody));
        }
    }
}

function submitForm(e, form, renderTarget, redirectOnSuccess = true, extraData = {}) {
    e.preventDefault();

    var url = form.attr('action');
    console.log("Submitting.." + url);

    var formData = new FormData(form[0]);
    var postData = new FormData();
    for (var pair of formData.entries()) {
        postData.append(pair[0], pair[1])
    }
    for (var key in extraData) {
        postData.append(key, extraData[key])
    }
    console.log(extraData)

    return $.ajax({
        type: "POST",
        url: url,

        data: postData,
        processData: false,
        contentType: false,
        success: function (data, textStatus, response) {
            if (renderTarget.attr('id') == 'register')
                renderTarget.html(response.responseText);
                
            if (redirectOnSuccess)
                location.href = location.href
        },
        error: function (response) {
            if (response.status === 422) { // Input validation failure, reload the partial view form into the modal view
                renderTarget.html(response.responseText);
            } else {
                errorModal.modal('toggle');
                var ret = response.responseText ? response.responseText : response;
                errorModalBody.html(ret);
                console.log(ret)
            }
        }
    });
}

async function submitBoth(e, targetModal) {
    console.log("submitBoth()")

    var modalBody = targetModal.find('.modal-body');
    var registerForm = modalBody.find("#registerForm");
    var mainForm = modalBody.find('form').not("#registerForm");
    var validateOnly = {"validateOnly": "true"}

    var registerResult;
    var mainResult;

    submitForm(e, mainForm, modalBody, false, validateOnly)
        .fail((jqXHR, response) => {
            if (jqXHR.status == 422) {
                registerResult = submitForm(e, registerForm, $("#register"), false, validateOnly);
            }
        })
        .done(() => {
            // Second validation
            registerResult = submitForm(e, registerForm, $("#register"), false, validateOnly)
                .done(() => {
                    // Final submissions
                    registerResult = submitForm(e, registerForm, $("#register"), false)
                        .done(() => {
                            submitForm(e, mainForm, modalBody, true, registerResult.responseJSON);
                        })
                });
        }).always(() => {
                // Update handlers on forms after AJAX requests update HTML
                updateModalPageForms(targetModal);
                $(targetModalId).animate({ scrollTop: 0 }, 50);
            }
        );
}