$(function() {
    var scriptSrc = $('script[src*=createViewer]');
    var documentID = scriptSrc.attr('data-document-id');

    $('#viewerContainer').pccViewer({
      documentID: documentID,
      imageHandlerUrl: '/pas-proxy',     // Base path the viewer should use to make requests to PAS (PrizmDoc Application Services).
      viewerAssetsPath: 'viewer-assets', // Base path the viewer should use for static assets
      resourcePath: 'viewer-assets/img', // Base path the viewer should use for images
      language: viewerCustomizations.languages['en-US'],
      template: viewerCustomizations.template,
      icons: viewerCustomizations.icons,
      annotationsMode: "LayeredAnnotations", // Use the new "LayeredAnnotations" system, which will persist annotation data as JSON (instead of the default "LegacyAnnotations" system, which uses a different XML format)
      redactionReasons: {
        enableRedactionReasonSelection: true, // Enable the UI to allow users to select a redaction reason.
        enableFreeformRedactionReasons: true, // Allow users to type a custom redaction reason.
        enableMultipleRedactionReasons: true, // Allow users to apply multiple redaction reasons to a single redaction (requires a backend running version 13.13 or higher)

        // TODO: Define your own set of redaction reasons for your users to pick from:
        reasons: [{
          reason: '1.a',                   // Text to apply to the redaction itself.
          description: 'Client Privilege'  // Optional extended description the user will see when choosing from the list of redaction reasons.
        }, {
          reason: '1.b',
          description: 'Privacy Information'
        }, {
          reason: '1.c'
        }]
      },
      uiElements: {
        attachments: true,                 // Enable the email attachments UI
        advancedSearch: true               // Enable advanced search
      },
      immediateActionMenuMode: "hover",    // Enable immediate action menu
      attachmentViewingMode: "ThisViewer", // The email attachment will be opened in the same view
    });
  });