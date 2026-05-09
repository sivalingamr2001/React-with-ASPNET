import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import { 
  AppBar, 
  Toolbar, 
  Typography, 
  Container,
  Button
} from '@mui/material';
import QueryManager from './components/QueryManager';
import QueryEditor from './components/QueryEditor';
import FetchJsonQueryBuilder from './components/FetchJsonQueryBuilder';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1976d2',
    },
    secondary: {
      main: '#dc004e',
    },
  },
});

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Router>
        <AppBar position="static">
          <Toolbar>
            <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
              KAT Reader Service - Query Builder
            </Typography>
            <Button color="inherit" href="/">Queries</Button>
            <Button color="inherit" href="/fetchjson">FetchJSON Builder</Button>
          </Toolbar>
        </AppBar>
        
        <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
          <Routes>
            <Route path="/" element={<QueryManager />} />
            <Route path="/editor" element={<QueryEditor />} />
            <Route path="/editor/:queryNumber" element={<QueryEditor />} />
            <Route path="/fetchjson" element={<FetchJsonQueryBuilder />} />
          </Routes>
        </Container>
      </Router>
    </ThemeProvider>
  );
}

export default App;