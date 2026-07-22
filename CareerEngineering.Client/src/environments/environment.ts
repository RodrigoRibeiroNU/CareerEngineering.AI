/**
 * Ambiente de produção.
 * Preencha com as URLs e credenciais Auth0 do deploy antes do `ng build --configuration=production`.
 */
export const environment = {
  production: true,
  apiUrl: 'https://YOUR_API_HOST/api',
  hubUrl: 'https://YOUR_API_HOST/careerChatHub',
  auth0: {
    domain: 'YOUR_AUTH0_DOMAIN',
    clientId: 'YOUR_AUTH0_CLIENT_ID',
    audience: 'YOUR_AUTH0_AUDIENCE',
  },
};
