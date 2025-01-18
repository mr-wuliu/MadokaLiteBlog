window.renderMath = async function () {
    if (typeof window.MathJax === 'undefined') {
        await new Promise((resolve) => {
            window.MathJax = {
                tex: {
                    inlineMath: [['$', '$'], ['\\(', '\\)']],
                    displayMath: [['$$', '$$'], ['\\[', '\\]']],
                    processEscapes: true,
                    packages: ['base', 'ams', 'noerrors', 'noundefined']
                },
                svg: {
                    fontCache: 'global'
                },
                startup: {
                    pageReady: () => {
                        resolve();
                    }
                }
            };

            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-svg.js';
            script.async = true;
            document.head.appendChild(script);
        });
    }

    try {
        await MathJax.typesetPromise();
        console.log('Math rendered successfully');
    } catch (error) {
        console.error('Error rendering math:', error);
    }
}