import $ = require('jquery');
import * as toastr from 'toastr';
require('tooltipster');
import { showButtonSpinner, clearForm, wrapCardCallback, get, wrapCardIconCallback, updateCardStatus, expandAllListener, collapseAllListener, filterTreeListener, filterFormListener, updateCardStatusButtons, updateFormStatusButtons, post } from '../shared';
import { ClientCard, RootContentItemCard, AddRootContentItemActionCard } from '../card';
import { FormBase } from '../form/form-base';
import { AccessMode } from '../form/form-modes';
import { ClientTree, RootContentItemList, RootContentItemSummary, BasicNode, ClientSummary, RootContentItemDetail, ContentType, PublishRequest, RootContentItemSummaryAndDetail } from '../view-models/content-publishing';
import { setUnloadAlert } from '../unload-alerts';
import { DeleteRootContentItemDialog, DiscardConfirmationDialog, CancelContentPublicationRequestDialog } from '../dialog';
import { SubmissionGroup } from '../form/form-submission';
import { PublicationStatusMonitor } from './publication-status-monitor';

export namespace ContentPublishingDOMMethods {

  let formObject: FormBase;
  let statusMonitor: PublicationStatusMonitor;

  function deleteRootContentItem(rootContentItemId: string, rootContentItemName: string, password: string, callback: () => void) {
    $.ajax({
      type: 'DELETE',
      url: 'ContentPublishing/DeleteRootContentItem',
      data: {
        rootContentItemId: rootContentItemId,
      },
      headers: {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
      }
    }).done(function onDone(response: RootContentItemDetail) {
      $('#content-publishing-form').hide();
        $('#root-content-items .card-container')
          .filter((i, card) => $(card).data().rootContentItemId === response.Id)
          .remove();
      addToDocumentCount(response.ClientId, -1);
      callback();
      toastr.success(rootContentItemName + ' was successfully deleted.');
    }).fail(function onFail(response) {
      callback();
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }
  export function rootContentItemDeleteClickHandler(event) {
    var $clickedCard = $(this).closest('.card-container');
    var rootContentItemId = $clickedCard.data().rootContentItemId;
    var rootContentItemName = $clickedCard.find('.card-body-primary-text').first().text();
    event.stopPropagation();
    new DeleteRootContentItemDialog(
      rootContentItemName,
      rootContentItemId,
      function (data, callback) {
        if (data.password) {
          showButtonSpinner($('.vex-first'), 'Deleting');
          $('.vex-dialog-button').attr('disabled', '');
          deleteRootContentItem(rootContentItemId, rootContentItemName, data.password, callback);
        } else if (data.password === '') {
          toastr.warning('Please enter your password to proceed');
          return false;
        } else {
          toastr.info('Deletion was canceled');
        }
        return true;
      },
    ).open();
  }
  function cancelContentPublication(data, callback) {
    $.ajax({
      type: 'POST',
      url: 'ContentPublishing/CancelContentPublicationRequest',
      data: {
        RootContentItemId: data.RootContentItemId,
      },
      headers: {
        RequestVerificationToken: $("input[name='__RequestVerificationToken']").val().toString()
      }
    }).done(() => {
      if (typeof callback === 'function') callback();
      toastr.success('Content publication request canceled');
    }).fail((response) => {
      if (typeof callback === 'function') callback();
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }

  export function rootContentItemCancelClickHandler(event) {
    var $clickedCard = $(this).closest('.card-container');
    var rootContentItemId = $clickedCard.data().rootContentItemId;
    var rootContentItemName = $clickedCard.find('.card-body-primary-text').first().text();
    event.stopPropagation();
    new CancelContentPublicationRequestDialog(rootContentItemId, rootContentItemName, cancelContentPublication).open();
  }
  export function rootContentItemGoLiveClickHandler(event) {
    var $clickedCard = $(this).closest('.card-container');
    var rootContentItemId = $clickedCard.data().rootContentItemId;
    var rootContentItemName = $clickedCard.find('.card-body-primary-text').first().text();
    event.stopPropagation();
    // do something
  }
  export function openNewRootContentItemForm() {
    const clientId = $('#client-tree [selected]').parent().data().clientId;
    renderRootContentItemForm({
      ClientId: clientId,
      ContentName: '',
      ContentTypeId: 0,
      Description: '',
      DoesReduce: false,
      Id: 0,
      Notes: '',
    });
    setFormNew();
  }


  function setFormReadOnly() {
    formObject.accessMode = AccessMode.Read;
    $('#root-content-items [selected]').removeAttr('editing');
    $('#content-publishing-form .admin-panel-toolbar .action-icon').show();
  }
  function setFormNew() {
    formObject.submissionMode = 'new';
    formObject.accessMode = AccessMode.Write;
    $('#root-content-items [selected]').attr('editing', '');
    $('#content-publishing-form .admin-panel-toolbar .action-icon').hide();
    $('#content-publishing-form .admin-panel-toolbar .action-icon-cancel').show();
  }
  function setFormEdit() {
    formObject.submissionMode = 'edit';
    formObject.accessMode = AccessMode.Defer;
    $('#root-content-items [selected]').attr('editing', '');
    $('#content-publishing-form .admin-panel-toolbar .action-icon').hide();
    $('#content-publishing-form .admin-panel-toolbar .action-icon-cancel').show();
  }
  function setFormEditOrRepublish() {
    formObject.submissionMode = 'edit-or-republish';
    formObject.accessMode = AccessMode.Write;
    $('#root-content-items [selected]').attr('editing', '');
    $('#content-publishing-form .admin-panel-toolbar .action-icon').hide();
    $('#content-publishing-form .admin-panel-toolbar .action-icon-cancel').show();
  }

  function mapRootContentItemDetail(item: RootContentItemDetail) {
    const formMap = new Map<string, string | number | boolean>();

    formMap.set('Id',item.Id);
    formMap.set('ClientId',item.ClientId);
    formMap.set('ContentName',item.ContentName);
    formMap.set('ContentTypeId',item.ContentTypeId);
    formMap.set('DoesReduce', item.DoesReduce);
    formMap.set('Description',item.Description);
    formMap.set('Notes',item.Notes);

    return formMap;
  }

  function addToDocumentCount(clientId: number, offset: number) {
    const itemCount = $('#client-tree .card-container')
      .filter((i, card) => $(card).data().clientId === clientId)
      .find('use[href="#action-icon-reports"]').closest('div').find('h4');
    itemCount.html(`${parseInt(itemCount.html()) + offset}`);
  }

  function renderRootContentItemForm(item?: RootContentItemDetail) {
    const $panel = $('#content-publishing-form');
    const $rootContentItemForm = $panel.find('form.admin-panel-content');

    if (item) {
      const formMap = mapRootContentItemDetail(item);
      formMap.forEach((value, key) => {
        $rootContentItemForm.find(`#${key}`).val(value ? value.toString() : '');
      });

      const $doesReduceToggle = $rootContentItemForm.find(`#DoesReduce`);
      $doesReduceToggle.prop('checked', item.DoesReduce);
    }

    const createContentGroup = new SubmissionGroup<RootContentItemSummaryAndDetail>(
      [
        'common',
        'root-content-item-info',
        'root-content-item-description',
      ],
      'ContentPublishing/CreateRootContentItem',
      'POST',
      (response) => {
        // Rerender the form to set Id and reset original values
        renderRootContentItemForm(response.detail);
        // Add the new content item as a card and select it
        renderRootContentItem(response.summary);
        $('#root-content-items .card-container')
          .filter((i, card) => $(card).data().rootContentItemId === response.detail.Id)
          .children().click();
        // Update the root content item count stat on the client card
        addToDocumentCount(response.detail.ClientId, 1);

        toastr.success('Root content item created');
      },
      (data) => {
        if (data.indexOf('DoesReduce=') === -1) {
          return data + '&DoesReduce=False';
        } else {
          return data.replace('DoesReduce=', '').replace('&&', '&') + '&DoesReduce=True';
        }
      },
    );
    const updateContentGroup = new SubmissionGroup<RootContentItemSummaryAndDetail>(
      [
        'common',
        'root-content-item-info',
        'root-content-item-description',
      ],
      'ContentPublishing/UpdateRootContentItem',
      'POST',
      (response) => {
        renderRootContentItemForm(response.detail);
        setFormReadOnly();
        // Update related root content item card
        const $card = $('#root-content-items .card-container')
          .filter((i, card) => $(card).data().rootContentItemId === response.detail.Id);
        $card.find('.card-body-primary-text').html(response.summary.ContentName);
        $card.find('.card-body-secondary-text').html(response.summary.ContentTypeName);
        toastr.success('Root content item updated');
      },
      (data) => {
        if (data.indexOf('DoesReduce=') === -1) {
          return data + '&DoesReduce=False';
        } else {
          return data.replace('DoesReduce=', '').replace('&&', '&') + '&DoesReduce=True';
        }
      },
    );
    const submitPublication = new SubmissionGroup<any>(
      [
        'common',
        'publication-files',
      ],
      'ContentPublishing/Publish',
      'POST',
      (response) => {
        renderRootContentItemForm();
        setFormReadOnly();
        // Update root content item card status immediately
        statusMonitor.checkStatus();
        toastr.success('Publication request submitted');
      },
      (data) => {
        let dataArray: { [key: string]: string } = {};
        data.split('&')
          .map((kvp) => kvp.split('='))
          .forEach((kvp) => dataArray[kvp[0]] = kvp[1]);
        const publishRequest: PublishRequest = {
          RootContentItemId: parseInt(dataArray['Id']),
          RelatedFiles: ['MasterContent', 'UserGuide', 'Thumbnail', 'ReleaseNotes']
            .map((file) => ({
              FilePurpose: file,
              FileUploadId: dataArray[file],
            }))
            .filter((file) => file.FileUploadId),
        };
        return publishRequest;
      },
    );

    // Create/retrieve and bind the new form
    formObject = new FormBase();
    formObject.bindToDOM($rootContentItemForm[0]);
    formObject.configure(
      [
        {
          groups: [ createContentGroup, submitPublication ],
          name: 'new',
          sparse: false,
        },
        {
          groups: [ updateContentGroup, submitPublication ],
          name: 'edit-or-republish',
          sparse: true,
        },
        {
          groups: [ updateContentGroup ],
          name: 'edit',
          sparse: false,
        },
      ],
    );
  }


  function renderRootContentItem(item: RootContentItemSummary) {
    const $panel = $('#content-publishing-form');
    const $card = new RootContentItemCard(
      item,
      item.GroupCount,
      item.EligibleUserCount,
      wrapCardCallback(get(
        'ContentPublishing/RootContentItemDetail',
        [
          updateFormStatusButtons,
          renderRootContentItemForm,
          setFormReadOnly,
        ],
      ), () => formObject),
      wrapCardIconCallback(($card, always) => get(
          'ContentPublishing/RootContentItemDetail',
          [
            renderRootContentItemForm,
            always,
          ],
        )($card), () => formObject, 1, undefined, () => {
        setFormEditOrRepublish();
      }),
      rootContentItemDeleteClickHandler,
      rootContentItemCancelClickHandler,
      rootContentItemGoLiveClickHandler,
    ).build();
    updateCardStatus($card, item.PublicationDetails);
    updateCardStatusButtons($card, item.PublicationDetails && item.PublicationDetails.StatusEnum);
    $card.data('statusEnum', item.PublicationDetails && item.PublicationDetails.StatusEnum);
    $('#root-content-items ul.admin-panel-content').append($card);
  }
  function renderRootContentItemList(response: RootContentItemList, rootContentItemId?: number) {
    const $rootContentItemList = $('#root-content-items ul.admin-panel-content');
    $rootContentItemList.empty();
    response.DetailList.forEach(renderRootContentItem);
    $rootContentItemList.find('.tooltip').tooltipster();

    if (!isNaN(rootContentItemId)) {
      $(`[data-root-content-item-id=${rootContentItemId}]`).click();
    }
  }


  function renderClientNode(client: BasicNode<ClientSummary>, level: number = 0) {
    const $card = new ClientCard(
      client.Value,
      client.Value.EligibleUserCount,
      client.Value.RootContentItemCount,
      level,
      wrapCardCallback(get(
        'ContentPublishing/RootContentItems',
        [ renderRootContentItemList ],
      )),
    );
    $card.disabled = !client.Value.CanManage;
    $('#client-tree ul.admin-panel-content').append($card.build());

    // Render child nodes
    client.Children.forEach((childNode) => {
      renderClientNode(childNode, level + 1);
    });
  }
  function renderClientTree(response: ClientTree, clientId?: number) {
    const $clientTreeList = $('#client-tree ul.admin-panel-content');
    $clientTreeList.empty();
    response.Root.Children.forEach((rootClient) => {
      renderClientNode(rootClient);
      $clientTreeList.append('<li class="hr width-100pct"></li>');
    });
    $clientTreeList.find('.hr').last().remove();
    $clientTreeList.find('.tooltip').tooltipster();

    if (!isNaN(clientId)) {
      $(`[data-client-id=${clientId}]`).click();
    }
  }

  function populateAvailableContentTypes(contentTypes: Array<ContentType>) {
    const $panel = $('#content-publishing-form');
    const $rootContentItemForm = $panel.find('form.admin-panel-content');

    const $contentTypeDropdown = $rootContentItemForm.find('#ContentTypeId');
    $contentTypeDropdown.children(':not(option[value = ""])').remove();

    contentTypes.forEach((contentType) => {
      const option = new Option(contentType.Name, contentType.Id.toString());
      $(option).data(contentType);
      $contentTypeDropdown.append(option);
    });

    $contentTypeDropdown.val(0);
    // $contentTypeDropdown.change(); // trigger change event
  }

  export function setup() {
    const $contentTypeDropdown = $('#ContentTypeId');
    $contentTypeDropdown.change(() => {
      const $doesReduceToggle = $('#DoesReduce');
      const contentType = $contentTypeDropdown
        .find(`option[value="${$contentTypeDropdown.val()}"]`)
        .data() as ContentType;
      if (!contentType.CanReduce) {
        $doesReduceToggle.attr('disabled', '');
        $doesReduceToggle.prop('checked', false);
      } else {
        $doesReduceToggle.removeAttr('disabled');
      }
    });

    $('.action-icon-expand').click(expandAllListener);
    $('.action-icon-collapse').click(collapseAllListener);
    $('.admin-panel-searchbar-tree').keyup(filterTreeListener);
    $('.admin-panel-searchbar-form').keyup(filterFormListener);

    $('#root-content-items .admin-panel-toolbar .action-icon-add').click(function () {
      openNewRootContentItemForm();
      $('#root-content-items .card-body-container').removeAttr('selected');
      $('#root-content-items .card-body-container.action-card').attr('selected', '');
      $('#content-publishing-form').show();
    })
    $('#root-content-items ul.admin-panel-content-action')
      .append(new AddRootContentItemActionCard(
        wrapCardCallback(openNewRootContentItemForm, () => formObject)
      ).build());

    $('.admin-panel-toolbar .action-icon-cancel').click(() => {
      if (formObject.accessMode === AccessMode.Read) {
        $('#root-content-items [selected]').click();
      } else {
        setFormReadOnly();
      }
    });
    $('.admin-panel-toolbar .action-icon-edit').click(() => {
      setFormEdit();
    });
    $('.admin-panel-toolbar .action-icon-file-upload').click(() => {
      setFormEditOrRepublish();
    });

    setUnloadAlert(() => formObject && formObject.modified);

    $('.tooltip').tooltipster();

    get(
      'ContentPublishing/AvailableContentTypes',
      [ populateAvailableContentTypes ],
    )();
    get(
      'ContentPublishing/Clients',
      [ renderClientTree ],
    )();

    statusMonitor = new PublicationStatusMonitor();
    statusMonitor.start();


    // TODO: delete this. for testing only.
    $('#client-tree > input').click(() => {
      $.ajax({
        method: 'GET',
        url: 'ContentPublishing/PreLiveSummary',
        data: {
            RootContentItemId: 1,
        }
      }).done((response) => {
        toastr.success('Success');
      }).fail((response) => {
        toastr.error('Error');
      }).always((response) => {
      });
    });
  }
}
