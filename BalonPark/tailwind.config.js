/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    "./Pages/**/*.{cshtml,razor,html}",
    "./Views/**/*.{cshtml,razor,html}",
    "./**/*.cshtml",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        primary: '#00A8E8',
        'primary-dark': '#0087c4',
        secondary: '#FFC300',
        support: '#2ECC71',
        accent: '#E63946',
        ink: '#222222',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};
