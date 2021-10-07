// fetch helpers
export function getJsonData<TResponse = any>(url = '', data: any = {}) {
  const queryParams: string[] = [];
  Object.keys(data).forEach((key) => {
    if (Object.prototype.hasOwnProperty.call(data, key)) {
      queryParams.push(`${key}=${data[key]}`);
    }
  });
  url = `${url}?${queryParams.join('&')}`;
  return fetch(url, {
    method: 'GET',
    cache: 'no-cache',
    credentials: 'same-origin',
  })
  .then((response) => {
    if (!response.ok) {
      throw new Error(response.headers.get('Warning') || `${response.status}`);
    } else if (response.redirected && response.url.match(/\/Account\/LogIn/) !== null) {
      throw new Error('sessionExpired');
    }
    return response.json() as Promise<TResponse>;
  });
}

export function postData(url: string = '', data: any = {}, rawResponse: boolean = false) {
  const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').getAttribute('value');
  const formData = Object.keys(data).map((key) => {
    if (Object.prototype.hasOwnProperty.call(data, key)) {
      return `${encodeURIComponent(key)}=${encodeURIComponent(data[key])}`;
    }
    return null;
  }).filter((kvp) => kvp !== null).join('&');
  return fetch(url, {
    method: 'POST',
    cache: 'no-cache',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
      'RequestVerificationToken': antiforgeryToken,
    },
    credentials: 'same-origin',
    body: formData,
  })
  .then((response) => {
    if (!response.ok) {
      throw new Error(response.headers.get('Warning') || 'Unknown error');
    }
    return rawResponse
      ? response
      : response.json();
  });
}

export function postJsonData<TResponse = any>(url: string = '', data: object = {}, method = 'POST') {
  const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').getAttribute('value');
  return fetch(url, {
    method,
    cache: 'no-cache',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      'RequestVerificationToken': antiforgeryToken,
    },
    credentials: 'same-origin',
    body: JSON.stringify(data),
  })
  .then((response) => {
    if (!response.ok) {
      throw new Error(response.headers.get('Warning') || `${response.status}`);
    } else if (response.redirected && response.url.match(/\/Account\/LogIn/) !== null) {
      throw new Error('sessionExpired');
    }
    return response.json() as Promise<TResponse>;
  });
}

export function postJsonDataNoSession(url: string = '', data: object = {}, method = 'POST') {
  const antiforgeryToken = document.querySelector('input[name="__RequestVerificationToken"]').getAttribute('value');
  return fetch(url, {
    method,
    cache: 'no-cache',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      'RequestVerificationToken': antiforgeryToken,
    },
    credentials: 'same-origin',
    body: JSON.stringify(data),
  })
    .then((response) => {
      if (!response.ok) {
        throw new Error(response.headers.get('Warning') || `${response.status}`);
      }
      return response;
    });
}

export function isStringNotEmpty(value: string): boolean {
  return value !== null && value.trim() !== '';
}

export function isDomainNameValid(domainName: string): boolean {
  const domainNameRegex =
    /^((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
  return domainName !== null && domainName.trim() !== '' && domainNameRegex.test(domainName);
}

export function isDomainNameProhibited(domainName: string, prohibitedDomains: string[]): boolean {
  const domainIsProhibited = prohibitedDomains.map((domain) => domain.toLowerCase()).includes(domainName.toLowerCase());
  return domainIsProhibited;
}

export function isEmailAddressValid(email: string): boolean {
  const emailRegex = new RegExp([
    '^(([^<>()[\\]\\\\.,;:\\s@\']+(\\.[^ <>()[\\]\\\\.,;: \\s@\']+)*)',
    '|(\'.+\'))@((\\[[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\])|',
    '(([a-zA-Z\\-0-9]+\\.)+[a-zA-Z]{2,}))$',
  ].join(''));
  return email === null || email.trim() === '' || emailRegex.test(email);
}

export function getParameterByName(name: string, url = window.location.href) {
  name = name.replace(/[\[\]]/g, '\\$&');
  const regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)');
  const results = regex.exec(url);
  if (!results) {
    return null;
  }
  if (!results[2]) {
    return '';
  }
  return decodeURIComponent(results[2].replace(/\+/g, ' '));
}

export function enableButtonOnScrollBottom(scrollElement: HTMLElement, button: HTMLButtonElement) {
  if (scrollElement.scrollHeight - scrollElement.clientHeight > 5) {
    scrollElement.addEventListener('scroll', () => {
      if (scrollElement.scrollHeight - scrollElement.scrollTop - scrollElement.clientHeight < 5) {
        button.disabled = false;
      }
    });
  } else {
    button.disabled = false;
  }
}
