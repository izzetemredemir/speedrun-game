using System;
using TMPro;
using UnityEngine;
using Cinemachine;

namespace TPSBR.UI
{
	public class UIAgentSelectionView : UICloseView
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private CinemachineVirtualCamera _camera;
		[SerializeField]
		private UIList _agentList;
		[SerializeField]
		private UIButton _selectButton;
		[SerializeField]
		private TextMeshProUGUI _agentName;
		[SerializeField]
		private TextMeshProUGUI _agentDescription;
		[SerializeField]
		private string _agentNameFormat = "{0}";
		[SerializeField]
		private UIBehaviour _selectedAgentGroup;
		[SerializeField]
		private UIBehaviour _selectedEffect;
		[SerializeField]
		private AudioSetup _selectedSound;
		[SerializeField]
		private float _closeDelayAfterSelection = 0.5f;

		private string _previewAgent;

		// UIView INTERFACE

		protected override void OnInitialize()
		{
			base.OnInitialize();

			_agentList.SelectionChanged += OnSelectionChanged;
			_agentList.UpdateContent += OnListUpdateContent;

			_selectButton.onClick.AddListener(OnSelectButton);
		}

		private void OnListUpdateContent(int index, MonoBehaviour content)
		{
			var behaviour = content as UIBehaviour;
			var setup = Context.Settings.Agent.Agents[index];

			behaviour.Image.sprite = setup.Icon;
		}

		protected override void OnOpen()
		{
			base.OnOpen();

			_camera.enabled = true;
			_selectedEffect.SetActive(false);

			_previewAgent = Context.PlayerData.AgentID;

			_agentList.Refresh(Context.Settings.Agent.Agents.Length, false);
			
			UpdateAgent();
		}

		protected override void OnClose()
		{
			_camera.enabled = false;

			Context.PlayerPreview.ShowAgent(Context.PlayerData.AgentID);

			base.OnClose();
		}

		protected override void OnDeinitialize()
		{
			_agentList.SelectionChanged -= OnSelectionChanged;
			_agentList.UpdateContent -= OnListUpdateContent;

			_selectButton.onClick.RemoveListener(OnSelectButton);

			base.OnDeinitialize();
		}

		// PRIVATE METHODS

		private void OnSelectionChanged(int index)
		{
			_previewAgent = Context.Settings.Agent.Agents[index].ID;
			UpdateAgent();
		}

		private void OnSelectButton()
		{
			bool isSame = Context.PlayerData.AgentID == _previewAgent;

			if (isSame == false)
			{
				Context.PlayerData.AgentID = _previewAgent;

				_selectedEffect.SetActive(false);
				_selectedEffect.SetActive(true);

				PlaySound(_selectedSound);

				UpdateAgent();
				Invoke("CloseWithBack", _closeDelayAfterSelection);
			}
			else
			{
				CloseWithBack();
			}
		}

		private void UpdateAgent()
		{
			var agentSetups = Context.Settings.Agent.Agents;
			_agentList.Selection = Array.FindIndex(agentSetups, t => t.ID == _previewAgent);

			if (_agentList.Selection < 0)
			{
				_agentList.Selection = 0;
				_previewAgent = agentSetups[_agentList.Selection].ID;
			}

			if (_previewAgent.HasValue() == false)
			{
				Context.PlayerPreview.HideAgent();
				_agentName.text = string.Empty;
				_agentDescription.text = string.Empty;
			}
			else
			{
				var setup = Context.Settings.Agent.GetAgentSetup(_previewAgent);

				Context.PlayerPreview.ShowAgent(_previewAgent);
				_agentName.text = string.Format(_agentNameFormat, setup.DisplayName);
				_agentDescription.text = setup.Description;
			}

			_selectedAgentGroup.SetActive(_previewAgent == Context.PlayerData.AgentID);
		}
	}
}
