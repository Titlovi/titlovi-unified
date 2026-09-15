define(['loading'], function (loading) {
    'use strict';

    var pluginId = '1dbf2e1e-3187-4f63-9a60-4ca7ace4232e';

    function load(page) {
        loading.show();
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            page.querySelector('#TitloviUsername').value = config.Username || '';
            page.querySelector('#TitloviPassword').value = config.Password || '';
            page.querySelector('#IncludeBshAlternativeLanguages').checked = config.IncludeBshAlternativeLanguages === true;
            page.querySelector('#AcceptSrt').checked = config.AcceptSrt !== false;
            page.querySelector('#AcceptSub').checked = config.AcceptSub !== false;
            page.querySelector('#AcceptTxt').checked = config.AcceptTxt !== false;
            page.querySelector('#AcceptSsa').checked = config.AcceptSsa !== false;
            page.querySelector('#AcceptAss').checked = config.AcceptAss !== false;
            page.querySelector('#ConvertDownloadedSubtitlesToUtf8').checked = config.ConvertDownloadedSubtitlesToUtf8 === true;
            page.querySelector('#EnableDebugLogging').checked = config.EnableDebugLogging === true;
            loading.hide();
        }).catch(function () {
            loading.hide();
            Dashboard.processErrorResponse({ statusText: 'Failed to load Titlovi.com configuration' });
        });
    }

    function save(e) {
        e.preventDefault();
        e.stopPropagation();
        loading.show();
        var form = this;

        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            var username = form.querySelector('#TitloviUsername').value.trim();
            var password = form.querySelector('#TitloviPassword').value.trim();
            var acceptSrt = form.querySelector('#AcceptSrt').checked;
            var acceptSub = form.querySelector('#AcceptSub').checked;
            var acceptTxt = form.querySelector('#AcceptTxt').checked;
            var acceptSsa = form.querySelector('#AcceptSsa').checked;
            var acceptAss = form.querySelector('#AcceptAss').checked;

            if (!username || !password) {
                loading.hide();
                Dashboard.processErrorResponse({ statusText: 'Enter both Titlovi.com username and password.' });
                return;
            }

            if (!acceptSrt && !acceptSub && !acceptTxt && !acceptSsa && !acceptAss) {
                loading.hide();
                Dashboard.processErrorResponse({ statusText: 'Select at least one accepted subtitle format.' });
                return;
            }

            if (config.Username !== username || config.Password !== password) {
                config.Token = null;
            }

            config.Username = username;
            config.Password = password;
            config.IncludeBshAlternativeLanguages = form.querySelector('#IncludeBshAlternativeLanguages').checked;
            config.AcceptSrt = acceptSrt;
            config.AcceptSub = acceptSub;
            config.AcceptTxt = acceptTxt;
            config.AcceptSsa = acceptSsa;
            config.AcceptAss = acceptAss;
            config.ConvertDownloadedSubtitlesToUtf8 = form.querySelector('#ConvertDownloadedSubtitlesToUtf8').checked;
            config.EnableDebugLogging = form.querySelector('#EnableDebugLogging').checked;

            ApiClient.updatePluginConfiguration(pluginId, config).then(function (result) {
                loading.hide();
                Dashboard.processPluginConfigurationUpdateResult(result);
            }).catch(function () {
                loading.hide();
                Dashboard.processErrorResponse({ statusText: 'Failed to save Titlovi.com configuration' });
            });
        });
        return false;
    }

    return function (view) {
        view.querySelector('form').addEventListener('submit', save);
        view.addEventListener('viewshow', function () { load(view); });
    };
});
