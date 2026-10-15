async function saveConfiguration(view, pluginId) {
    Dashboard.showLoadingMsg();

    try {
        const config = await ApiClient.getPluginConfiguration(pluginId);
        const username = view.querySelector('#TitloviUsername').value.trim();
        const password = view.querySelector('#TitloviPassword').value.trim();
        const acceptSrt = view.querySelector('#AcceptSrt').checked;
        const acceptSub = view.querySelector('#AcceptSub').checked;
        const acceptTxt = view.querySelector('#AcceptTxt').checked;
        const acceptSsa = view.querySelector('#AcceptSsa').checked;
        const acceptAss = view.querySelector('#AcceptAss').checked;

        if (!username || !password) {
            throw new Error('Enter both Titlovi.com username and password.');
        }

        if (!acceptSrt && !acceptSub && !acceptTxt && !acceptSsa && !acceptAss) {
            throw new Error('Select at least one accepted subtitle format.');
        }

        if (config.Username !== username || config.Password !== password) {
            config.Token = null;
        }

        config.Username = username;
        config.Password = password;
        config.IncludeBshAlternativeLanguages = view.querySelector('#IncludeBshAlternativeLanguages').checked;
        config.AcceptSrt = acceptSrt;
        config.AcceptSub = acceptSub;
        config.AcceptTxt = acceptTxt;
        config.AcceptSsa = acceptSsa;
        config.AcceptAss = acceptAss;
        config.ConvertDownloadedSubtitlesToUtf8 = view.querySelector('#ConvertDownloadedSubtitlesToUtf8').checked;
        config.EnableDebugLogging = view.querySelector('#EnableDebugLogging').checked;

        const result = await ApiClient.updatePluginConfiguration(pluginId, config);
        Dashboard.processPluginConfigurationUpdateResult(result);
    } catch (error) {
        Dashboard.processErrorResponse({
            statusText: error && error.message ? error.message : 'Failed to save Titlovi.com configuration'
        });
    } finally {
        Dashboard.hideLoadingMsg();
    }
}

async function loadConfiguration(view, pluginId) {
    Dashboard.showLoadingMsg();

    try {
        const config = await ApiClient.getPluginConfiguration(pluginId);
        view.querySelector('#TitloviUsername').value = config.Username || '';
        view.querySelector('#TitloviPassword').value = config.Password || '';
        view.querySelector('#IncludeBshAlternativeLanguages').checked = config.IncludeBshAlternativeLanguages === true;
        view.querySelector('#AcceptSrt').checked = config.AcceptSrt !== false;
        view.querySelector('#AcceptSub').checked = config.AcceptSub !== false;
        view.querySelector('#AcceptTxt').checked = config.AcceptTxt !== false;
        view.querySelector('#AcceptSsa').checked = config.AcceptSsa !== false;
        view.querySelector('#AcceptAss').checked = config.AcceptAss !== false;
        view.querySelector('#ConvertDownloadedSubtitlesToUtf8').checked = config.ConvertDownloadedSubtitlesToUtf8 === true;
        view.querySelector('#EnableDebugLogging').checked = config.EnableDebugLogging === true;
    } catch (error) {
        Dashboard.processErrorResponse({ statusText: 'Failed to load Titlovi.com configuration' });
    } finally {
        Dashboard.hideLoadingMsg();
    }
}

export default function (view) {
    const pluginId = '6e25df50-638e-4109-a50b-03c14fc93fdd';
    const form = view.querySelector('#JellyfinTitloviConfigForm');

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        event.stopPropagation();
        void saveConfiguration(view, pluginId);
        return false;
    });

    view.addEventListener('viewshow', function () {
        void loadConfiguration(view, pluginId);
    });
}
