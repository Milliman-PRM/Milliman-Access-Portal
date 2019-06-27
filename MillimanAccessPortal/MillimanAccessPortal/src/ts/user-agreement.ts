import { convertMarkdownToHTML } from './convert-markdown';
import { postData } from './shared';

import '../../src/scss/disclaimer.scss';

document.addEventListener('DOMContentLoaded', () => {
    const rawMarkdown = document.getElementById('raw-markdown').innerText;
    const contentDisclaimer = document.getElementById('disclaimer-text');
    contentDisclaimer.innerHTML = convertMarkdownToHTML(rawMarkdown);

    handleScroll();
});

function handleScroll() {
    const element = document.getElementById('disclaimer-container');
    const acceptButton = document.getElementById('accept-button');

    if (element.scrollHeight - element.scrollTop === element.clientHeight) {
        (acceptButton as HTMLButtonElement).disabled = false;

        acceptButton.onclick = async () => {
            try {
                await postData('/Account/AcceptUserAgreement', {
                    AgreementText: (document.getElementById('AgreementText') as HTMLInputElement).value,
                    IsRenewal: (document.getElementById('IsRenewal') as HTMLInputElement).value,
                    ReturnUrl: (document.getElementById('ReturnUrl') as HTMLInputElement).value,
                }, true)
                    .then((response) => {
                        const redirectUrl = response.headers.get('NavigateTo');
                        window.location.replace(redirectUrl);
                    });
            } catch (e) {
                alert('An error has occurred. Please try again.');
            }
        };
    }
}
