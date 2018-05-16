import $ = require('jquery');
require('tooltipster');
import { showButtonSpinner, clearForm, wrapCardCallback, get, wrapCardIconCallback, updateCardStatus, expandAllListener, collapseAllListener, filterTreeListener, filterFormListener } from '../shared';
import { ClientCard, RootContentItemCard, AddRootContentItemActionCard } from '../card';
import { FormBase } from '../form/form-base';
import { AccessMode } from '../form/form-modes';
import { ClientTree, RootContentItemList, RootContentItemSummary, BasicNode, ClientSummary, RootContentItemDetail, ContentType } from '../view-models/content-publishing';
import { setUnloadAlert } from '../unload-alerts';
import { DeleteRootContentItemDialog, DiscardConfirmationDialog } from '../dialog';
import { SubmissionGroup } from '../form/form-submission';

export namespace ContentPublishingDOMMethods {

  const forms = new Map<number, FormBase>();
  let currentForm: FormBase;
  let currentFormId: number;

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
    }).done(function onDone(response) {
      $('#root-content-items .admin-panel-content').empty();
      $('#content-publishing-form').hide();
      renderRootContentItemList(response);
      callback();
      toastr.success(rootContentItemName + ' was successfully deleted.');
    }).fail(function onFail(response) {
      callback();
      toastr.warning(response.getResponseHeader('Warning'));
    });
  }
  export function rootContentItemDeleteClickHandler() {
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
    currentForm.accessMode = AccessMode.Read;
    $('#root-content-items [selected]').removeAttr('editing');
    $('#content-publishing-form .admin-panel-toolbar .action-icon').show();
  }
  function setFormNew() {
    currentForm.submissionMode = 'new';
    currentForm.accessMode = AccessMode.Write;
    $('#root-content-items [selected]').attr('editing', '');
    $('#content-publishing-form .admin-panel-toolbar .action-icon').hide();
    $('#content-publishing-form .admin-panel-toolbar .action-icon-cancel').show();
  }
  function setFormEdit() {
    currentForm.submissionMode = 'edit';
    currentForm.accessMode = AccessMode.Defer;
    $('#root-content-items [selected]').attr('editing', '');
    $('#content-publishing-form .admin-panel-toolbar .action-icon').show();
    $('#content-publishing-form .admin-panel-toolbar .action-icon-edit').hide();
  }
  function setFormRepublish() {
    currentForm.submissionMode = 'republish';
    currentForm.accessMode = AccessMode.Defer;
    $('#root-content-items [selected]').attr('editing', '');
    $('#content-publishing-form .admin-panel-toolbar .action-icon').show();
    $('#content-publishing-form .admin-panel-toolbar .action-icon-file-upload').hide();
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

  function renderRootContentItemForm(item: RootContentItemDetail) {
    const $panel = $('#content-publishing-form');
    const $rootContentItemForm = $panel.find('form.admin-panel-content');
    clearForm($panel);

    const formMap = mapRootContentItemDetail(item);
    formMap.forEach((value, key) => {
      $rootContentItemForm.find(`#${key}`).val(value ? value.toString() : '');
    });

    const $doesReduceToggle = $rootContentItemForm.find(`#DoesReduce`);
    $doesReduceToggle.prop('checked', item.DoesReduce);

    const createContentGroup = new SubmissionGroup<RootContentItemDetail>(
      [
        'common',
        'root-content-item-info',
        'root-content-item-description',
      ],
      'ContentPublishing/CreateRootContentItem',
      'POST',
      (response) => $('#Id').val(response.Id),
    );
    const updateContentGroup = new SubmissionGroup<RootContentItemDetail>(
      [
        'common',
        'root-content-item-info',
        'root-content-item-description',
      ],
      'ContentPublishing/UpdateRootContentItem',
      'POST',
      (response) => renderRootContentItemForm(response),
    );
    const submitPublication = new SubmissionGroup<any>(
      [
        'common',
        'publication-files',
      ],
      'ContentPublishing/Publish',
      'POST',
      (response) => { },
    );

    // First unbind existing form if it exists
    if (currentForm) {
      currentForm.unbindFromDOM();
      forms.set(currentFormId, currentForm);
    }

    // Create/retrieve and bind the new form
    if (!forms.has(item.Id)) {
      currentForm = new FormBase();
      currentForm.bindToDOM($rootContentItemForm[0]);
      currentForm.configure(
        [
          {
            group: createContentGroup/*.chain(submitPublication)*/,
            name: 'new',
          },
          {
            group: updateContentGroup,
            name: 'edit',
          },
          {
            group: submitPublication,
            name: 'republish',
          },
        ],
      );
    } else {
      currentForm = forms.get(item.Id);
      currentForm.bindToDOM($rootContentItemForm[0]);
    }
    
    setFormReadOnly();
    currentFormId = item.Id;
  }


  function renderRootContentItem(item: RootContentItemSummary) {
    const $card = new RootContentItemCard(
      item,
      item.GroupCount,
      item.EligibleUserCount,
      wrapCardCallback(get(
        'ContentPublishing/RootContentItemDetail',
        [ renderRootContentItemForm ],
      ), () => currentForm),
      wrapCardIconCallback(($card, whenDone) => get(
          'ContentPublishing/RootContentItemDetail',
          [
            renderRootContentItemForm,
            whenDone
          ],
        )($card), () => currentForm, 1, undefined, () => {
        setFormRepublish();
      }),
      wrapCardIconCallback(($card, whenDone) => get(
          'ContentPublishing/RootContentItemDetail',
          [
            renderRootContentItemForm,
            whenDone
          ],
        )($card), () => currentForm, 1, undefined, () => {
        setFormEdit();
      }),
      rootContentItemDeleteClickHandler,
    ).build();
    updateCardStatus($card, item.PublicationDetails);
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
        wrapCardCallback(openNewRootContentItemForm, () => currentForm)
      ).build());

    $('.admin-panel-toolbar .action-icon-edit').click(() => {
      setFormEdit();
    });
    $('.admin-panel-toolbar .action-icon-cancel').click(() => {
      if (currentForm.accessMode === AccessMode.Read) {
        $('#root-content-items [selected]').click();
      } else {
        setFormReadOnly();
      }
    });
    $('.admin-panel-toolbar .action-icon-file-upload').click(() => {
      setFormRepublish();
    });

    setUnloadAlert(() => currentForm && currentForm.modified);

    $('.tooltip').tooltipster();

    get(
      'ContentPublishing/AvailableContentTypes',
      [ populateAvailableContentTypes ],
    )();
    get(
      'ContentPublishing/Clients',
      [ renderClientTree ],
    )();
  }
}
