// Global Toast Alert Helper
function showAlert(type, title, message) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    
    // Determine the Lottie file path based on alert type
    let animationFile = '';
    if (type === 'success') animationFile = 'template/success.json';
    else if (type === 'error') animationFile = 'template/error.json';
    else if (type === 'warning') animationFile = 'template/Alert.json';

    toast.innerHTML = `
        <div class="toast-icon" id="lottie-${Date.now()}"></div>
        <div class="toast-content">
            <h4>${title}</h4>
            <p>${message}</p>
        </div>
    `;

    container.appendChild(toast);

    // Initialize the Lottie animation for this specific toast
    const iconContainer = toast.querySelector('.toast-icon');
    if (animationFile && typeof lottie !== 'undefined') {
        lottie.loadAnimation({
            container: iconContainer,
            renderer: 'svg',
            loop: false, // Plays the tick/cross animation once
            autoplay: true,
            path: animationFile
        });
    }

    // Auto dismiss after 3.5 seconds
    setTimeout(() => {
        toast.classList.add('hide');
        setTimeout(() => toast.remove(), 400); // Wait for CSS animation
    }, 3500);
}
