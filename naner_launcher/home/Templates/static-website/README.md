# {{PROJECT_NAME}}

A static website built with HTML, CSS, and JavaScript, created with Naner.

## Getting Started

### Option 1: Python HTTP Server
```bash
python -m http.server 8000
```

### Option 2: Node.js HTTP Server
```bash
npx http-server
```

### Option 3: Open directly
Simply open `index.html` in your web browser.

## Tech Stack

- **HTML5** - Structure
- **CSS3** - Styling with modern features (CSS Grid, Flexbox, CSS Variables)
- **JavaScript** - Interactivity and smooth scrolling

## Features

- Responsive design
- Smooth scrolling navigation
- Contact form
- Modern CSS with CSS Variables
- Mobile-friendly

## Project Structure

```
{{PROJECT_NAME}}/
├── index.html      # Main HTML file
├── styles.css      # Stylesheet
├── script.js       # JavaScript functionality
└── README.md       # This file
```

## Customization

### Change Colors
Edit CSS variables in `styles.css`:
```css
:root {
    --primary-color: #646cff;  /* Change this */
    --text-color: #333;
    --background-color: #f5f5f5;
}
```

### Add Sections
Add new sections in `index.html`:
```html
<section id="newsection" class="content">
    <div class="container">
        <h2>New Section</h2>
        <p>Content goes here</p>
    </div>
</section>
```

### Add JavaScript Functionality
Add your functions in `script.js`.

## Deployment

### GitHub Pages
1. Push to GitHub repository
2. Go to Settings → Pages
3. Select branch and save
4. Your site will be live at `https://username.github.io/repo-name`

### Netlify
1. Sign up at [Netlify](https://www.netlify.com/)
2. Drag and drop your project folder
3. Your site goes live instantly

### Vercel
```bash
npm i -g vercel
vercel
```

## Learn More

- [MDN Web Docs](https://developer.mozilla.org/)
- [CSS Tricks](https://css-tricks.com/)
- [JavaScript.info](https://javascript.info/)
