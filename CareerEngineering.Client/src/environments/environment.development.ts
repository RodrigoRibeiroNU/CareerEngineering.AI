/**
 * Ambiente de desenvolvimento local (`ng serve` / build development).
 */
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5019/api',
  hubUrl: 'http://localhost:5019/careerChatHub',
  auth0: {
    domain: 'dev-41nhlxtdpvk10ged.us.auth0.com',
    clientId: 'E6M23rVZb8kKlt3GwHjaDB6bOB3pa0O5',
    audience: 'https://careerengineering-api.com',
  },
};
