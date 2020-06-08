﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;


/*
	This class handles mostly all of the buttons and controls
	for the application, including toggling between drawing modes,
	changing colors and brush types, etc.
*/
public class PaintController : MonoBehaviour, PlacenoteListener {

	public GameObject drawingRootSceneObject;
	public Text textLabel;
	private bool pointCloudOn = false;

	public GameObject paintPanel;
	public GameObject startPanel;

    private GameObject buttonPanel;
	private bool snapToSurfaceEnabled;
	private int activeLayerNum = 1;

    [SerializeField] GameObject brushTipObject;
	[SerializeField] GameObject brushTipGraphic;
	[SerializeField] GameObject snapToSurfaceBrushTipObject;
    [SerializeField] GameObject colorPalette;
    [SerializeField] GameObject mainButtonPanel;
	[SerializeField] GameObject moveLayerPanel;
    [SerializeField] GameObject modePanel;


    [SerializeField] GameObject setActiveLayerPanel;
    [SerializeField] GameObject showHideLayerPanel;
    [SerializeField] GameObject saveLoadLayerPanel;

    public enum DrawingMode
    {
    	none,
    	normal,
    	surface,
    	feature
    }

    [SerializeField] RawImage mLocalizationThumbnail;
    [SerializeField] Image mLocalizationThumbnailContainer;

    public int drawingHistoryIndex = 0;
    public DrawingMode currentDrawingMode;


	// Initialization
	void Start () {
        LibPlacenote.Instance.RegisterListener (this);

        mLocalizationThumbnailContainer.gameObject.SetActive(false);

        // Set up the localization thumbnail texture event.
        LocalizationThumbnailSelector.Instance.TextureEvent += (thumbnailTexture) =>
        {
            if (mLocalizationThumbnail == null)
            {
                return;
            }

            // set the width and height of the thumbnail based on the texture obtained
            RectTransform rectTransform = mLocalizationThumbnailContainer.rectTransform;
            if (thumbnailTexture.width != (int)rectTransform.rect.width)
            {
                rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal, thumbnailTexture.width * 2);
                rectTransform.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical, thumbnailTexture.height * 2);
                rectTransform.ForceUpdateRectTransforms();
            }

            // set the texture
            mLocalizationThumbnail.texture = thumbnailTexture;
        };


        // Make sure panels match the defaults
        startPanel.SetActive (true);
        mainButtonPanel.SetActive(true);

        modePanel.SetActive(false);
		paintPanel.SetActive (false);
		colorPalette.SetActive(false);
		brushTipObject.SetActive(false);
		snapToSurfaceBrushTipObject.SetActive(false);
		snapToSurfaceEnabled = false;

		setActiveLayerPanel.SetActive(false);
        showHideLayerPanel.SetActive(false);
        saveLoadLayerPanel.SetActive(false);

		// Make sure this child is active for when its parent is active
		buttonPanel = paintPanel.transform.Find("ButtonPanel").gameObject;
		buttonPanel.SetActive(true);

		currentDrawingMode = DrawingMode.normal;
    }


    public int GetActiveLayerNum()
    {
    	return activeLayerNum;
    }


    // Toggle the controls for color palette, brush types, etc.
    public void OnToggleColorPaletteClick()
    {
        if (colorPalette.activeInHierarchy)
        {
            colorPalette.SetActive(false);
        }
        else
        {
            colorPalette.SetActive(true);
        }
    }


    // Called every frame
    // We have no need for this here
    void Update () {}		


    // Start the drawing session
	public void onStartPaintingClick ()
	{
		if (!LibPlacenote.Instance.Initialized()) {
			textLabel.text = "Please wait for the SDK to be initialized...";
			return;
		}

		startPanel.SetActive (false);
		paintPanel.SetActive (true);

        onClearAllClick();

        LibPlacenote.Instance.StartSession ();

        brushTipObject.SetActive(true);

        textLabel.text = "Press and hold the screen to paint";
	}

	public void TogglePanelSetActiveLayer(bool panelIsActive)
	{
		setActiveLayerPanel.SetActive(panelIsActive);
		mainButtonPanel.SetActive(!panelIsActive);
	}

	public void TogglePanelShowHideLayer(bool panelIsActive)
	{
		showHideLayerPanel.SetActive(panelIsActive);
		mainButtonPanel.SetActive(!panelIsActive);
	}

	public void TogglePanelSaveLoadLayer(bool panelIsActive)
	{
		saveLoadLayerPanel.SetActive(panelIsActive);
		mainButtonPanel.SetActive(!panelIsActive);
	}

	public void TogglePanelMoveLayer(bool panelIsActive)
	{
		moveLayerPanel.SetActive(panelIsActive);
		mainButtonPanel.SetActive(!panelIsActive);
	}

	public void TogglePanelMode(bool panelIsActive)
	{
		modePanel.SetActive(panelIsActive);
        mainButtonPanel.SetActive(!panelIsActive);
	}


	// Sets a layer as the active layer
	public void OnSetActiveLayerClick(int layerNum) {
		activeLayerNum = layerNum;
	}


	// Shows a particular layer
	public void OnShowLayerClick(int layerNum) {
		Debug.Log("OnShowLayerClick not yet implemented");
		ShowLayer(layerNum);
	}


	// Hides a particular layer
	public void OnHideLayerClick(int layerNum) {
		Debug.Log("OnHideLayerClick not yet implemented");
		HideLayer(layerNum);
	}


	// Handle moving a layer
	public void OnMoveLayerClick(int layerNum)
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		textLabel.text = "Moving Layer " + layerNum;

		// Clear from the screen
		Clearlayer(layerNum);

		// Get the new position
		Vector3 pos = GetComponent<DrawLineManager>().getNewPositionForLayer(0.3f);

		// Let the history move and re-redner it
		GetComponent<DrawingHistoryManager>().moveLayer(layerNum, pos);
		textLabel.text = "Layer " + layerNum + " moved!";
	}


	// Save this drawing as a layer
	public void OnSaveLayerClick (int layerNum)
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		textLabel.text = "Saving Layer " + layerNum;
		GetComponent<DrawingHistoryManager>().saveLayer(layerNum);
		textLabel.text = "Layer " + layerNum + " saved!";
	}


	// Load a saved drawing layer
	public void OnLoadLayerClick (int layerNum)
	{
		if (!LibPlacenote.Instance.Initialized()) {
			Debug.Log ("SDK not yet initialized");
			return;
		}

		textLabel.text = "Loading Layer " + layerNum;
		GetComponent<DrawingHistoryManager>().loadLayer(layerNum);
		textLabel.text = "Layer " + layerNum + " loaded!";
	}


	// Handles changing the drawing mode
	public void OnModeClick(string mode)
	{
		// Handle turning off the current mode
		switch (currentDrawingMode) {
			case DrawingMode.normal:
				ToggleModeNormal(false);
				break;
			case DrawingMode.feature:
				ToggleModeFeature(false);
				break;
			case DrawingMode.surface:
				ToggleModeSurface(false);
				break;
			default:
				break;
		}

		// Handle turning on the new mode
		if (mode == "normal") {
			ToggleModeNormal(true);
		}
		else if (mode == "feature") {
			ToggleModeFeature(true);
		}
		else if (mode == "surface") {
			ToggleModeSurface(true);
		}
		else {
			Debug.Log("Invalid mode passed to OnModeClick: " + mode);
		}

		TogglePanelMode(false);
	}


	// Toggle the normal drawing mode
	private void ToggleModeNormal(bool modeNormalOn) {
		if (modeNormalOn) {
			// Turn on
			currentDrawingMode = DrawingMode.normal;
			textLabel.text = "Press the Screen to Paint";
		} else {
			// Turn off
			currentDrawingMode = DrawingMode.none;
		}
	}


	// Toggle the Snap to Surface drawing mode
	// When user clicks snap to surface button, activate snap to surface panel
	// and snap to surface brush tip object. On return to main click, deactivate.
	private void ToggleModeSurface(bool modeSurfaceOn) {
		if (modeSurfaceOn) {
			// Turn on
			if (!LibPlacenote.Instance.Initialized()) {
				Debug.Log ("SDK not yet initialized");
				return;
			}
			currentDrawingMode = DrawingMode.surface;
			
			textLabel.text = "Snap to Surface Enabled";

	        snapToSurfaceBrushTipObject.SetActive(true);
			brushTipObject.SetActive(false);
			GetComponent<ReticleController>().StartReticle();
		} else {
			// Turn off
			currentDrawingMode = DrawingMode.none;
			textLabel.text = "Returning to Main Session";
			snapToSurfaceBrushTipObject.SetActive(false);
			brushTipObject.SetActive(true);
			GetComponent<ReticleController>().StopReticle();
		}
	}


	// Toggle the Feature Point drawing mode
	private void ToggleModeFeature(bool modeFeatureOn) {
		if (modeFeatureOn) {
			// Turn on
			currentDrawingMode = DrawingMode.feature;
			if (pointCloudOn == false) {
				FeaturesVisualizer.EnablePointcloud(new Color(1f, 1f, 1f, 0.2f), new Color(1f, 1f, 1f, 0.8f));
				pointCloudOn = true;
				Debug.Log ("Point Cloud On");
			}
			GetComponent<FeatureHighlightController>().ToggleHighlight(true);
			textLabel.text = "Highlight a Feature and Tap the Screen to Connect";
		} else {
			// Turn off
			currentDrawingMode = DrawingMode.none;
			if (pointCloudOn == true) {
				FeaturesVisualizer.DisablePointcloud ();
	            pointCloudOn = false;
				Debug.Log ("Point Cloud Off");
			}
			GetComponent<FeatureHighlightController>().ToggleHighlight(false);
		}
	}


	// Shows a layer
	public void ShowLayer(int layerNum)
	{
		GraphicsLineRenderer[] lines = drawingRootSceneObject.transform.GetComponentsInChildren<GraphicsLineRenderer>(true);
		for (int i = 0; i < lines.Length; i++) {
			if (lines[i].GetLayerNum() == layerNum) {
				lines[i].gameObject.SetActive(true);
			}
		}
	}


	// Hides a layer
	public void HideLayer(int layerNum)
	{
		GraphicsLineRenderer[] lines = drawingRootSceneObject.transform.GetComponentsInChildren<GraphicsLineRenderer>(false);
		for (int i = 0; i < lines.Length; i++) {
			if (lines[i].GetLayerNum() == layerNum) {
				lines[i].gameObject.SetActive(false);
			}
		}
	}


	// Removes a layer from the screen
	private void Clearlayer(int layerNum)
	{
		GraphicsLineRenderer[] lines = drawingRootSceneObject.transform.GetComponentsInChildren<GraphicsLineRenderer>(false);
		for (int i = 0; i < lines.Length; i++) {
			if (lines[i].GetLayerNum() == layerNum) {
				Destroy(lines[i].gameObject);
			}
		}
	}


	// Deletes all parts of the drawing
	public void deleteAllObjects()
	{
		int numChildren = drawingRootSceneObject.transform.childCount;

		for (int i = 0; i < numChildren; i++) {

			GameObject toDestroy = drawingRootSceneObject.transform.GetChild (i).gameObject;

			if (string.Compare (toDestroy.name, "CubeBrushTip") != 0  && string.Compare (toDestroy.name, "SphereBrushTip") != 0   ) {
				Destroy (drawingRootSceneObject.transform.GetChild (i).gameObject);
			}
		}

	}


	// Clear the drawing and history of it
	public void onClearAllClick()
	{
		deleteAllObjects ();
		GetComponent<DrawingHistoryManager>().resetHistory ();
	}


	// This function runs when LibPlacenote sends a status change message like Localized!
	// This is mostly used for debugging
	public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
	{
		Debug.Log ("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());

		if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {

			Debug.Log ("Localized!");

		} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
			Debug.Log ("Mapping");

		} else if (currStatus == LibPlacenote.MappingStatus.LOST) {
			Debug.Log("Searching for position lock");

		} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {

		}
	}

	// Some functions for LibPlacenote that we didn't have a use for
	public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}
    public void OnLocalized() {}
}
