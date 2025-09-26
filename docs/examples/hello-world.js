(function() {
    'use strict';

    // Wait for Vivaldi's UI to be ready
    function waitForVivaldi() {
        if (typeof vivaldi === 'undefined' || !vivaldi.tabs) {
            setTimeout(waitForVivaldi, 100);
            return;
        }
        initMod();
    }

    function initMod() {
        // Listen for tab creation events
        vivaldi.tabs.onCreated.addListener(function(tab) {
            // Only show popup for new empty tabs (not restored tabs or specific URLs)
            if (tab.url === 'chrome://newtab/' || tab.url === 'vivaldi://newtab/') {
                showHelloWorldPopup();
            }
        });
    }

    function showHelloWorldPopup() {
        // Create popup overlay
        const overlay = document.createElement('div');
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0, 0, 0, 0.5);
            display: flex;
            justify-content: center;
            align-items: center;
            z-index: 10000;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        `;

        // Create popup content
        const popup = document.createElement('div');
        popup.style.cssText = `
            background-color: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
            text-align: center;
            max-width: 400px;
            animation: slideIn 0.3s ease-out;
        `;

        // Add CSS animation
        const style = document.createElement('style');
        style.textContent = `
            @keyframes slideIn {
                from { transform: translateY(-50px); opacity: 0; }
                to { transform: translateY(0); opacity: 1; }
            }
        `;
        document.head.appendChild(style);

        // Popup content
        popup.innerHTML = `
            <h2 style="margin: 0 0 15px 0; color: #333; font-size: 24px;">
                Hello World! 🌍
            </h2>
            <p style="margin: 0 0 20px 0; color: #666; font-size: 16px;">
                Your Vivaldi mod is working perfectly!<br>
                This popup appears when you open a new tab.
            </p>
            <button id="closePopup" style="
                background-color: #ef3939;
                color: white;
                border: none;
                padding: 10px 20px;
                border-radius: 5px;
                cursor: pointer;
                font-size: 14px;
                transition: background-color 0.2s;
            ">
                Close
            </button>
        `;

        overlay.appendChild(popup);
        document.body.appendChild(overlay);

        // Close popup functionality
        const closeBtn = popup.querySelector('#closePopup');
        const closePopup = () => {
            overlay.style.animation = 'fadeOut 0.2s ease-in';
            setTimeout(() => {
                if (overlay.parentNode) {
                    overlay.parentNode.removeChild(overlay);
                }
            }, 200);
        };

        closeBtn.addEventListener('click', closePopup);
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) {
                closePopup();
            }
        });

        // Auto-close after 5 seconds
        setTimeout(closePopup, 5000);

        // Add fade out animation
        style.textContent += `
            @keyframes fadeOut {
                from { opacity: 1; }
                to { opacity: 0; }
            }
        `;

        // Hover effect for button
        closeBtn.addEventListener('mouseenter', function() {
            this.style.backgroundColor = '#d73027';
        });
        closeBtn.addEventListener('mouseleave', function() {
            this.style.backgroundColor = '#ef3939';
        });
    }

    // Start the mod
    waitForVivaldi();
})();