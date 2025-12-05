/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.razor",
    "./**/*.html",
    "./**/*.cshtml",
    "../StoreApp.Client/**/*.{razor,html,cshtml}",
    "./Pages/**/*.{razor,html}",
    "./Shared/**/*.{razor,html}",
    "./wwwroot/**/*.html"
  ],

  safelist: [
    'bg-[#4ade80]',
    'hover:bg-[#16a34a]',
    'bg-[#16a34a]',
  ],

  theme: {
    extend: {
      colors: {
        primary: '#4ade80',
        primaryDark: '#22c55e',
        primaryHover: '#16a34a',
        primaryLight: '#86efac',

        success: {
          DEFAULT: '#10b981',
        },
        momo: {
          DEFAULT: '#d82d8b',
        },
        warning: {
          DEFAULT: '#f59e0b',
        },
        danger: {
          DEFAULT: '#ef4444',
        },
        paid: {
          DEFAULT: '#059669',
        },
        unpaid: {
          DEFAULT: '#dc2626',
        }
      },

      boxShadow: {
        soft: '0 10px 30px rgba(74,222,128,0.15)',
        strong: '0 20px 40px rgba(34,197,94,0.25)',
      }
    },
  },

  plugins: [],
}
