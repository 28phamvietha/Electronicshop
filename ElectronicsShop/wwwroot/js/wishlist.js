(function () {
    // Prevent double initialization if the script is included multiple times
    if (window.__wishlistBindingsAttached) {
        console.log('wishlist.js: bindings already attached');
        return;
    }
    window.__wishlistBindingsAttached = true;

    console.log('wishlist.js loaded');

    // Ensure jQuery is available
    if (typeof jQuery === 'undefined') {
        console.error('wishlist.js: jQuery not found');
        return;
    }

    var $ = jQuery;

    // Anti-forgery token (layout emits hidden token)
    var antiToken = $('input[name="__RequestVerificationToken"]').val() || null;

    var addUrl = window.wishlistAddUrl || '/Wishlist/Add';
    var removeUrl = window.wishlistRemoveUrl || '/Wishlist/Remove';
    var loginUrl = window.wishlistLoginUrl || '/Account/Login';

    function postAjax(url, data, cb) {
        if (antiToken) data.__RequestVerificationToken = antiToken;
        $.ajax({
            url: url,
            method: 'POST',
            data: data,
            success: cb,
            error: function (xhr, status, err) {
                console.log('wishlist AJAX error', status, err);
                // fallback: reload
                window.location.reload();
            }
        });
    }

    // Bind handlers directly to elements (not delegated) because .cart-dropdown
    // stops propagation in main.js, preventing document-level delegation from firing.
    function bindAddButtons() {
        $('.wishlist-add').off('click.wl').on('click.wl', function (e) {
            e.preventDefault();
            var $el = $(this);
            var productId = $el.data('productid');
            if (!productId) return;
            console.log('wishlist-add clicked, productId=', productId);

            postAjax(addUrl, { productId: productId }, function (res) {
                if (!res) { window.location.reload(); return; }
                if (res.redirectUrl) { window.location = res.redirectUrl; return; }
                if (res.success) {
                    $('.wishlist-count').text(res.wishlistCount);
                    if (res.html) {
                        $('#wishlist-dropdown .cart-dropdown').html(res.html);
                        // rebind remove buttons inside replaced HTML
                        bindRemoveButtons();
                    }
                    $el.find('i').removeClass('fa-heart-o').addClass('fa-heart');
                } else {
                    window.location.reload();
                }
            });
        });
    }

    function bindRemoveButtons() {
        $('.wishlist-remove').off('click.wl').on('click.wl', function (e) {
            e.preventDefault();
            console.log('wishlist-remove clicked (direct)');

            var $btn = $(this);
            var wishlistId = $btn.data('wishlistid');
            var productId = $btn.data('productid');

            if (!wishlistId) {
                console.log('wishlist.js: missing wishlistId');
                return;
            }

            postAjax(removeUrl, { wishlistId: wishlistId }, function (res) {
                console.log('wishlist remove response', res);
                if (!res) { window.location.reload(); return; }
                if (res.redirectUrl) { window.location = res.redirectUrl; return; }
                if (res.success) {
                    $('.wishlist-count').text(res.wishlistCount);
                    if (res.html) {
                        $('#wishlist-dropdown .cart-dropdown').html(res.html);
                        // rebind because dropdown HTML replaced
                        bindRemoveButtons();
                        bindAddButtons(); // in case add buttons are inside updated HTML
                    }
                    var removedPid = res.removedProductId || productId;
                    if (removedPid) {
                        var selector = '.wishlist-add[data-productid="' + removedPid + '"] i';
                        $(selector).removeClass('fa-heart').addClass('fa-heart-o');
                    }
                } else {
                    window.location.reload();
                }
            });
        });
    }

    // Initial binding on DOM ready
    $(function () {
        bindAddButtons();
        bindRemoveButtons();
    });

})();