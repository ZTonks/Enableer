import { Provider, teamsTheme } from '@fluentui/react-northstar';
import React from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import { AppRoute } from './router/router';

const container = document.getElementById('root');
const root = createRoot(container);
root.render(
  <Provider theme={teamsTheme}>
    <React.StrictMode>
      <AppRoute />
    </React.StrictMode>
  </Provider>
);